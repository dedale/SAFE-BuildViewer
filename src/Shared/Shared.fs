namespace Shared

open System

module Route =
    let hello = "/api/hello"

type Platform =
    Win32 | X64
    override x.ToString() =
        match x with
        | Win32 -> "Win32"
        | X64 -> "x64"

type Configuration = Debug | Release

[<AutoOpen>]
module Sha1 =
    type Sha1 =
        private { Value : string }
        override x.ToString() = x.Value

    let create str =
        let m = System.Text.RegularExpressions.Regex.Match(str, "^[a-z\d]{40}$")
        if m.Success then Ok { Sha1.Value = str }
        else Error "Sha1 should be 40 alpha num"

    let random = Random()

    let createRandom () =
        let value =
            seq { 1..5 }
            |> Seq.map (fun _ -> random.Next().ToString("x8"))
            |> String.Concat
        { Sha1.Value = value }

type BuildRequest =
    { Id : Guid
      User : string
      Sha1 : Sha1
      Platform : Platform
      Configuration : Configuration }
    static member create () =
        { Id = Guid.NewGuid()
          User = "ded"
          Sha1 = Sha1.createRandom()
          Platform = X64
          Configuration = Release }

type BuildProgress =
    { Server : string
      Start : DateTime
      ExpectedMin : int
      Errors : string list }
    static member create () =
        { Server = "VMBUILD01"
          Start = DateTime.UtcNow.AddMinutes(-9.0)
          ExpectedMin = 20
          Errors = [] }

type BuildStatus =
    | Requests of (BuildRequest * BuildProgress option) list
