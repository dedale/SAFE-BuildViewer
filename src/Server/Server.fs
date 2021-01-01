module Server

open FSharp.Control.Tasks.V2
open Giraffe
open Saturn

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
open Shared
open System

let webApp =
    router {
        get Route.hello (json "Hello from SAFE!")
    }

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
        use_gzip
        add_channel "/socket/data" channel
        start_custom_process (fun app ->
            task {
                let socketHub = app.ApplicationServices.GetService<ISocketHub>()
                let started = DateTime.UtcNow
                let rec loop () =
                    task {
                        let message = (DateTime.UtcNow - started).ToString("hh\:mm\:ss") |> sprintf "uptime is %s"
                        do! sendMessage socketHub "server" message
                        System.Threading.Thread.Sleep 2000
                        do! loop()
                    }
                do! loop()
            }
        )
    }

run app
