module Server

open FSharp.Control.Tasks.V2
open Saturn
open Shared

let loadBuilds () = Requests [
    BuildRequest.create(), None
    BuildRequest.create(), BuildProgress.create() |> Some
]

type BuildStatus with

    member x.add request =
        match x with
        | Requests requests ->
            (request, None) :: requests |> Requests

    member x.startBuild id progress =
        match x with
        | Requests requests ->
            requests |> List.map (fun (k, v) -> if k.Id = id then k, Some progress else k, v) |> Requests

    member x.addError id error =
        match x with
        | Requests requests ->
            requests
            |> List.map (fun (k, v) ->
                if k.Id = id then
                    match v with
                    | Some progress -> k, Some { progress with Errors = error :: progress.Errors }
                    | _ -> k, v
                else k, v)
            |> Requests

    member x.remove id =
        match x with
        | Requests requests ->
            requests |> List.filter (fun (k, v) -> k.Id <> id) |> Requests

let sendMessage (hub: Channels.ISocketHub) topic payload =
    task {
        printfn "sendMessage %s" topic
        let message = Thoth.Json.Net.Encode.Auto.toString(0, payload)
        do! hub.SendMessageToClients "/socket/data" topic message
    }

let channel =
    channel {
        join (fun ctx socketId ->
            task {
                printfn "Someone has connected!"
                return Channels.Ok
            }
        )
    }

module ApplicationExtension =
    open Microsoft.AspNetCore.Builder
    open System.Threading.Tasks

    type Saturn.Application.ApplicationBuilder with

        [<CustomOperation("start_custom_process")>]
        member __.StartCustomProcess(state: ApplicationState, startup: IApplicationBuilder -> Task<unit>) =
            
            let appBuilderConfig (app: IApplicationBuilder) =
                Task.Run<Task<unit>>((fun () -> startup app)) |> ignore
                app
            { state with AppConfigs = appBuilderConfig :: state.AppConfigs }

open ApplicationExtension
open Microsoft.Extensions.DependencyInjection
open Saturn.Channels

let app =
    application {
        url "http://0.0.0.0:8085"
        no_router
        memory_cache
        use_static "public"
        use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
        use_gzip
        add_channel "/socket/data" channel
        start_custom_process (fun app ->
            task {
                let socketHub = app.ApplicationServices.GetService<ISocketHub>()
                let send topic payload = sendMessage socketHub topic payload
                let mutable status = loadBuilds()
                let rec loop () =
                    task {
                        do! send "status" status

                        System.Threading.Thread.Sleep 2000
                        
                        let newRequest = BuildRequest.create()
                        status <- status.add newRequest
                        do! send "status" status

                        System.Threading.Thread.Sleep 2000

                        let start = BuildProgress.create()
                        status <- status.startBuild newRequest.Id start
                        do! send "status" status

                        System.Threading.Thread.Sleep 2000

                        let error = "error MS1234: failed"
                        status <- status.addError newRequest.Id error
                        do! send "status" status

                        System.Threading.Thread.Sleep 10000

                        status <- status.remove newRequest.Id

                        do! loop()
                    }
                do! loop()
            }
        )
    }

run app
