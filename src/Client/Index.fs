module Index

open Elmish
open Thoth.Fetch

open Shared

type Model =
    { Hello: string
      Status: BuildStatus }

type Msg =
    | GotHello of string

let init() =
    let model : Model =
        { Hello = ""
          Status = Requests [ BuildRequest.create(), None ] }
    let getHello() = Fetch.get<unit, string> Route.hello
    let cmd = Cmd.OfPromise.perform getHello () GotHello
    model, cmd

let update msg model =
    match msg with
    | GotHello hello ->
        { model with Hello = hello }, Cmd.none

open Feliz
open System

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

let renderRequest request =
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
        ]
    ]

let renderQueue model =
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
                    for r, _ in requests do
                        yield renderRequest r
            ]
        ]
    ]

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
                Html.h1 "safe_minimal"
                Html.h2 model.Hello
                renderQueue model
                renderProgress {
                    BuildProgress.Start = DateTime.UtcNow.AddMinutes(-11.0)
                    ExpectedMin = 20
                    Server = "VMBUILD01"
                    Errors = []
                }
                renderProgress {
                    BuildProgress.Start = DateTime.UtcNow.AddMinutes(-9.0)
                    ExpectedMin = 20
                    Server = "VMBUILD02"
                    Errors = [
                        "error MSB1234: failed"
                        "error LNK2019: unresolved external"
                    ]
                }
                renderProgress {
                    BuildProgress.Start = DateTime.UtcNow.AddMinutes(-25.0)
                    ExpectedMin = 20
                    Server = "VMBUILD12"
                    Errors = []
                }
            ]
        ]
    ]
