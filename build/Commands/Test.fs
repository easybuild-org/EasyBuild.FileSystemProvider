module EasyBuild.Commands.Test

open Spectre.Console.Cli
open SimpleExec
open EasyBuild.Workspace

type TestSettings() =
    inherit CommandSettings()

    [<CommandOption("-w|--watch")>]
    member val IsWatch = false with get, set

type TestCommand() =
    inherit Command<TestSettings>()
    interface ICommandLimiter<TestSettings>

    override __.Execute(context, settings) =

        if settings.IsWatch then
            Command.Run("dotnet", "watch test", workingDirectory = Workspace.tests.``.``)
        else
            Command.Run("dotnet", "test", workingDirectory = Workspace.tests.``.``)

        0
