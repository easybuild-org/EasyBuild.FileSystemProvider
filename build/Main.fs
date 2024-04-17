module EasyBuild.Main

open Spectre.Console.Cli
open EasyBuild.Commands.Test
open EasyBuild.Commands.Publish
open SimpleExec

[<EntryPoint>]
let main args =

    Command.Run("dotnet", "husky install")

    let app = CommandApp()

    app.Configure(fun config ->
        config.Settings.ApplicationName <- "./build.sh"

        config.AddCommand<TestCommand>("test") |> ignore
        config.AddCommand<PublishCommand>("publish") |> ignore
    )

    app.Run(args)
