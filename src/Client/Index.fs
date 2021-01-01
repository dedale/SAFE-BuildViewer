module Index

open Elmish
open Thoth.Fetch

open Shared

type Model =
    { Hello: string }

type Msg =
    | GotHello of string

let init() =
    let model : Model =
        { Hello = "" }
    let getHello() = Fetch.get<unit, string> Route.hello
    let cmd = Cmd.OfPromise.perform getHello () GotHello
    model, cmd

let update msg model =
    match msg with
    | GotHello hello ->
        { model with Hello = hello }, Cmd.none

open Feliz

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
            ]
        ]
    ]
