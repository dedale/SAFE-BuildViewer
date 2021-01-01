module Index

open Elmish
open System

open Shared

type Model =
    { Hello: string
      Status: BuildStatus
      ShownErrors: Guid option }

type Msg =
    | GotHello of string
    | ShowErrors of Guid
    | HideErrors

module Channel =

    open Browser.Types
    open Browser.WebSocket

    type ChannelMessage = { Topic: string; Payload: string }

    let inline decode<'a> m = m |> unbox<string> |> Thoth.Json.Decode.Auto.unsafeFromString<'a>

    let connect =
        fun dispatch ->
            let onWebSocketMessage (msg:MessageEvent) =
                let msg = msg.data |> decode<ChannelMessage>
                printfn "received %s" msg.Topic
                match msg.Topic with
                | "server" -> msg.Payload |> decode<string> |> GotHello |> dispatch
                | _ -> ()

            let rec connect () =
                let host = Browser.Dom.window.location.host
                let url = sprintf "ws://%s/socket/data" host
                let ws = WebSocket.Create(url)

                ws.onopen <- (fun _ ->
                    printfn "connection opened!"
                    ())
                ws.onclose <- (fun _ ->
                    printfn "connection closed!"
                    promise {
                        do! Promise.sleep 2000
                        connect()
                    }
                )
                ws.onmessage <- onWebSocketMessage

            connect()

        |> Cmd.ofSub

let init() =
    let model : Model =
        { Hello = ""
          Status = Requests [
            BuildRequest.create(), None
            BuildRequest.create(), BuildProgress.create() |> Some
            BuildRequest.create(), { BuildProgress.create() with Errors = [ "error MSB1234: failed" ] } |> Some
          ]
          ShownErrors = None }
    model, Channel.connect

let update msg model =
    match msg with
    | GotHello hello -> { model with Hello = hello }, Cmd.none
    | ShowErrors id -> { model with ShownErrors = Some id }, Cmd.none
    | HideErrors -> { model with ShownErrors = None }, Cmd.none

open Fable.FontAwesome
open Feliz

let renderProgress (progress : BuildProgress) =
    let elapsed = DateTime.UtcNow - progress.Start
    let elapsedMin = int elapsed.TotalMinutes
    let isLate = elapsedMin >= progress.ExpectedMin
    let color = if progress.Errors.Length = 0 then "green" else "red"
    let width = 200
    let leftWidth = if isLate then width else width * elapsedMin / progress.ExpectedMin
    let text = sprintf "%dm:%ss elapsed" elapsedMin (elapsed.Seconds.ToString("00"))
    Html.div [
        prop.className "progress-bar"
        prop.children [
            Html.div [
                prop.classes [ "bar"; "positive" ]
                prop.style [
                    style.width leftWidth
                    style.backgroundColor color
                ]
                prop.children [
                    Html.span [
                        prop.className "bar"
                        prop.style [
                            style.width width
                            style.color "white"
                        ]
                        prop.text text
                    ]
                ]
            ]
            if not isLate then Html.div [
                prop.classes [ "bar"; "negative" ]
                prop.style [ style.width (width - leftWidth) ]
                prop.children [
                    Html.span [
                        prop.className "bar"
                        prop.style [
                            style.width width
                            style.left -leftWidth
                            style.color color
                        ]
                        prop.text text
                    ]
                ]
            ]
        ]
    ]

let renderRequest request (progress : BuildProgress option) (model : Model) dispatch =
    let (bar, server, details) = 
        match progress with
        | None -> (Html.none, Html.none, Html.none)
        | Some p ->
            Html.td [ renderProgress p ],
            Html.td p.Server,
            if p.Errors.Length = 0
            then Html.td []
            else Html.td [
                Html.button [
                    prop.style [ style.color.red ]
                    prop.onClick (fun _ ->
                        match model.ShownErrors with
                        | Some id ->
                            if id = request.Id
                            then dispatch HideErrors
                            else dispatch (ShowErrors request.Id)
                        | None -> dispatch (ShowErrors request.Id))
                    prop.children [
                        Fa.i [ Fa.Solid.List ] []
                    ]
                ]
            ]
    Html.tr [
        prop.style [
            style.border (1, borderStyle.solid, "lightgray")
        ]
        prop.children [
            Html.td request.User
            Html.td [
                Html.a [
                    prop.style [ style.fontFamily "monospace" ]
                    prop.href (sprintf "http://github/owner/repo/commit/%A" request.Sha1)
                    prop.text (request.Sha1.ToString().Substring(0, 7))
                ]
            ]
            Html.td [
                prop.style [ style.fontFamily "monospace" ]
                prop.text (request.Platform.ToString())
            ]
            Html.td [
                prop.style [ style.fontFamily "monospace" ]
                prop.text (request.Configuration.ToString())
            ]
            bar
            server
            details
        ]
    ]

let renderErrors request (progress : BuildProgress option) (model : Model) =
    match progress with
    | Some p ->
        if p.Errors.Length = 0 then
            Html.none
        else
            match model.ShownErrors with
            | Some id ->
                if id = request.Id
                then Html.tr [
                        Html.td [
                            prop.colSpan 7
                            prop.children [
                                Html.p [
                                    prop.style [
                                        style.color.red
                                        style.textAlign.left
                                    ]
                                    prop.children [
                                        for e in p.Errors do
                                            yield Html.span e
                                            yield Html.br []
                                    ]
                                ]
                            ]
                        ]
                    ]
                else Html.none
            | _ -> Html.none
    | _ -> Html.none

let renderRequests filter model dispatch =
    Html.table [
        prop.style [
            style.marginLeft length.auto
            style.marginRight length.auto
            style.borderCollapse.collapse
        ]
        prop.children [
            Html.tbody [
                match model.Status with
                | Requests requests ->
                    let filtered = requests |> List.filter (fun (_, p) -> filter p)
                    for r, p in filtered do
                        yield renderRequest r p model dispatch
                        yield renderErrors r p model
            ]
        ]
    ]

let renderQueue model dispatch = renderRequests Option.isNone model dispatch

let renderPool model dispatch = renderRequests Option.isSome model dispatch

let view model dispatch =
    Html.div [ 
        prop.style [
            style.fontFamily "sans-serif"
            style.textAlign.center
            style.padding 40
        ]
        prop.children [
            Html.div [
                Html.img [ prop.src "favicon.png" ]
                Html.h2 model.Hello
                Html.h2 [
                    Fa.i [ Fa.Solid.Pause ] []
                ]
                renderQueue model dispatch
                Html.h2 [
                    Fa.i [ Fa.Solid.Play ] []
                ]
                renderPool model dispatch
            ]
        ]
    ]
