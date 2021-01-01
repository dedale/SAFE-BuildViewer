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
            ]
        ]
    ]
