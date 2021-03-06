﻿module FSDN.Program

open System
open System.IO
open System.Net
open Suave
open Suave.Web
open Suave.Logging
open Suave.Operators
open Suave.Filters
open Suave.Files
open Argu
open FSharpApiSearch

type Args =
  | Port of Sockets.Port
  | Home_Directory of string
  | Log_Level of string
with
  interface IArgParserTemplate with
    member this.Usage =
      match this with
      | Port _ -> "specify a primary port."
      | Home_Directory _ -> "specify a home or root diretory."
      | Log_Level _ -> "specify log level."

let logger (args: ParseResults<Args>) =
  match args.TryPostProcessResult(<@ Log_Level @>, LogLevel.FromString) with
  | Some l -> l
  | None -> LogLevel.Warn
  |> Loggers.ConsoleWindowLogger

let notFound homeDir ctx = asyncOption {
  let! ctx = browseFile homeDir "404.html" ctx
  return { ctx with response = { ctx.response with status = HTTP_404 } }
}

let fileRequest homeDir =
  let notFound = notFound homeDir
  choose [
    path "/" >=> browseFile homeDir "index.html"
    browseHome
  ]

let app database packages homeDir logger : WebPart =
  choose [
    GET >=> fileRequest homeDir
    HEAD >=> fileRequest homeDir
    Api.app database packages logger
    notFound homeDir
  ]
  >=> log logger logFormat

let serverConfig port homeDir logger = {
  defaultConfig with
    bindings = [ HttpBinding.mk HTTP IPAddress.Any port ]
    homeFolder = Some homeDir
    logger = logger
}

let parser = ArgumentParser.Create<Args>()

[<EntryPoint>]
let main args =
  let args = parser.Parse(args)
  let homeDir = DirectoryInfo(args.GetResult(<@ Home_Directory @>, ".")).FullName
  let logger = logger args
  let port = args.GetResult(<@ Port @>, 8083us)
  let config = serverConfig port homeDir logger
  let database =
    Path.Combine(homeDir, ApiLoader.databaseName)
    |> ApiLoader.loadFromFile
  let packages =
    Path.Combine(homeDir, "packages.yml")
    |> Package.load
    |> function
    | Some xs -> xs
    | None -> [||]
    |> Package.all
  let app = app database packages homeDir logger
  startWebServer config app
  0
