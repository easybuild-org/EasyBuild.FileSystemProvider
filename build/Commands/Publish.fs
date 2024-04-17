module EasyBuild.Commands.Publish

open Spectre.Console.Cli
open SimpleExec
open EasyBuild.Commands.Test
open LibGit2Sharp
open EasyBuild.Workspace
open System.Linq
open System.Text.RegularExpressions
open System
open System.IO
open BlackFox.CommandLine
open EasyBuild.Utils.Dotnet

type Type =
    | Feat
    | Fix
    | CI
    | Chore
    | Docs
    | Test
    | Style
    | Refactor

    static member fromText(text: string) =
        match text with
        | "feat" -> Feat
        | "fix" -> Fix
        | "ci" -> CI
        | "chore" -> Chore
        | "docs" -> Docs
        | "test" -> Test
        | "style" -> Style
        | "refactor" -> Refactor
        | _ -> failwith $"Invalid scope: {text}"

type CommitMessage =
    {
        Type: Type
        Scope: string option
        Description: string
        BreakingChange: bool
    }

let private parseCommitMessage (commitMsg: string) =
    let commitRegex =
        Regex(
            "^(?<type>feat|fix|ci|chore|docs|test|style|refactor)(\(?<scope>.+?\))?(?<breakingChange>!)?: (?<description>.{1,})$"
        )

    let m = commitRegex.Match(commitMsg)

    if m.Success then
        let scope =
            if m.Groups.["scope"].Success then
                Some m.Groups.["scope"].Value
            else
                None

        {
            Type = Type.fromText m.Groups.["type"].Value
            Scope = scope
            Description = m.Groups.["description"].Value
            BreakingChange = m.Groups.["breakingChange"].Success
        }

    else
        failwith
            $"Invalid commit message format.

Expected a commit message with the following format: <type>[optional scope]: <description>

Where <type> is one of the following:
- feat: A new feature
- fix: A bug fix
- ci: Changes to CI/CD configuration
- chore: Changes to the build process or external dependencies
- docs: Documentation changes
- test: Adding or updating tests
- style: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- refactor: A code change that neither fixes a bug nor adds a feature

Example:
-------------------------
feat: add new feature
-------------------------"

let capitalizeFirstLetter (text: string) =
    (string text.[0]).ToUpper() + text.[1..]

type PublishSettings() =
    inherit CommandSettings()

    [<CommandOption("--major")>]
    member val BumpMajor = false with get, set

    [<CommandOption("--minor")>]
    member val BumpMinor = false with get, set

    [<CommandOption("--patch")>]
    member val BumpPatch = false with get, set

type PublishCommand() =
    inherit Command<PublishSettings>()
    interface ICommandLimiter<PublishSettings>

    override __.Execute(context, settings) =

        // TODO: Replace libgit2sharp with using CLI directly
        // libgit2sharp seems all nice at first, but I find the API to be a bit cumbersome
        // when manipulating the repository for (commit, stage, etc.)
        // It also doesn't support SSH
        use repository = new Repository(Workspace.``.``)

        if repository.Head.FriendlyName <> "main" then
            failwith "You must be on the main branch to publish"

        if repository.RetrieveStatus().IsDirty then
            failwith "You have uncommitted changes"

        TestCommand().Execute(context, TestSettings()) |> ignore

        let changelogContent =
            File.ReadAllText(Workspace.``CHANGELOG.md``).Replace("\r\n", "\n").Split('\n')

        let changelogConfigSection =
            changelogContent
            |> Array.skipWhile (fun line -> "<!-- EasyBuild: START -->" <> line)
            |> Array.takeWhile (fun line -> "<!-- EasyBuild: END -->" <> line)

        let lastReleasedCommit =
            let regex = Regex("^<!-- last_commit_released:\s(?'hash'\w*) -->$")

            changelogConfigSection
            |> Array.tryPick (fun line ->
                let m = regex.Match(line)

                if m.Success then
                    Some m.Groups.["hash"].Value
                else
                    None
            )

        let commitFilter = CommitFilter()
        // If we found a last released commit, use it as the starting point
        // Otherwise, not providing a starting point seems to get all commits
        if lastReleasedCommit.IsSome then
            commitFilter.ExcludeReachableFrom <- lastReleasedCommit.Value

        let commits = repository.Commits.QueryBy(commitFilter).ToList()

        let releaseCommits =
            commits
            // Parse the commit to get the commit information
            |> Seq.map (fun commit ->
                {|
                    Commit = commit
                    CommitMessage = parseCommitMessage commit.MessageShort
                |}
            )
            // Remove commits that don't trigger a release
            |> Seq.filter (fun commit ->
                match commit.CommitMessage.Type with
                | Feat
                | Fix -> true
                | CI
                | Chore
                | Docs
                | Test
                | Style
                | Refactor -> false
            )

        if Seq.isEmpty releaseCommits then
            printfn "No commits found to make a release"
            0
        else

            let lastChangelogVersion = Changelog.tryGetLastVersion Workspace.``CHANGELOG.md``

            // Should user bump version take priority over commits infered version bump?
            // Should we make the user bump version mutually exclusive?

            let shouldBumpMajor =
                settings.BumpMajor
                || releaseCommits |> Seq.exists (fun commit -> commit.CommitMessage.BreakingChange)

            let shouldBumpMinor =
                settings.BumpMinor
                || releaseCommits |> Seq.exists (fun commit -> commit.CommitMessage.Type = Feat)

            let shouldBumpPatch =
                settings.BumpPatch
                || releaseCommits |> Seq.exists (fun commit -> commit.CommitMessage.Type = Fix)

            let refVersion =
                match lastChangelogVersion with
                | Some version -> version.Version
                | None -> Semver.SemVersion(0, 0, 0)

            let newVersion =
                if shouldBumpMajor then
                    refVersion.WithMajor(refVersion.Major + 1).WithMinor(0).WithPatch(0)
                elif shouldBumpMinor then
                    refVersion.WithMinor(refVersion.Minor + 1).WithPatch(0)
                elif shouldBumpPatch then
                    refVersion.WithPatch(refVersion.Patch + 1)
                else
                    failwith "No version bump required"

            let newVersionLines = ResizeArray()

            let appendLine (line: string) = newVersionLines.Add(line)

            let newLine () = newVersionLines.Add("")

            appendLine ($"## {newVersion}")
            newLine ()

            releaseCommits
            |> Seq.groupBy (fun commit -> commit.CommitMessage.Type)
            |> Seq.iter (fun (commitType, commitGroup) ->
                match commitType with
                | Feat -> "### 🚀 Features" |> appendLine
                | Fix -> "### 🐞 Bug Fixes" |> appendLine
                // Following types below are not included in the changelog
                | CI
                | Chore
                | Docs
                | Test
                | Style
                | Refactor -> ()

                newLine ()

                for commit in commitGroup do
                    let githubCommitUrl sha =
                        $"https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/%s{sha}"

                    let shortSha = commit.Commit.Sha.Substring(0, 7)
                    let commitUrl = githubCommitUrl commit.Commit.Sha

                    let description = capitalizeFirstLetter commit.CommitMessage.Description

                    $"- %s{description} ([%s{shortSha}](%s{commitUrl}))" |> appendLine
            )

            newLine ()

            // TODO: Add contributors list
            // TODO: Add breaking changes list

            let rec removeConsecutiveEmptyLines
                (previousLineWasBlank: bool)
                (result: string list)
                (lines: string list)
                =
                match lines with
                | [] -> result
                | line :: rest ->
                    // printfn $"%A{String.IsNullOrWhiteSpace(line)}"
                    if previousLineWasBlank && String.IsNullOrWhiteSpace(line) then
                        removeConsecutiveEmptyLines true result rest
                    else
                        removeConsecutiveEmptyLines
                            (String.IsNullOrWhiteSpace(line))
                            (result @ [ line ])
                            rest

            let newChangelogContent =
                [
                    // Add title and description of the original changelog
                    yield!
                        changelogContent
                        |> Seq.takeWhile (fun line -> "<!-- EasyBuild: START -->" <> line)

                    // Ad EasyBuild metadata
                    "<!-- EasyBuild: START -->"
                    $"<!-- last_commit_released: {commits[0].Sha} -->"
                    "<!-- EasyBuild: END -->"
                    ""

                    // New version
                    yield! newVersionLines

                    // Add the rest of the changelog
                    yield!
                        changelogContent |> Seq.skipWhile (fun line -> not (line.StartsWith("##")))
                ]
                |> removeConsecutiveEmptyLines false []
                |> String.concat "\n"

            File.WriteAllText(Workspace.``CHANGELOG.md``, newChangelogContent)

            let escapedPackageReleasesNotes =
                newVersionLines
                |> Seq.toList
                |> removeConsecutiveEmptyLines false []
                |> String.concat "\n"
                // Escape quotes and commas
                |> fun text -> text.Replace("\"", "\\\\\\\"").Replace(",", "%2c")

            // Clean up the src/bin folder
            if Directory.Exists VirtualWorkspace.src.bin.``.`` then
                Directory.Delete(VirtualWorkspace.src.bin.``.``, true)

            let struct (standardOutput, _) =
                Command.ReadAsync(
                    "dotnet",
                    CmdLine.empty
                    |> CmdLine.appendRaw "pack"
                    |> CmdLine.appendRaw Workspace.src.``EasyBuild.FileSystemProvider.fsproj``
                    |> CmdLine.appendRaw "-c Release"
                    |> CmdLine.appendRaw $"-p:PackageVersion=\"%s{newVersion.ToString()}\""
                    |> CmdLine.appendRaw
                        $"-p:PackageReleaseNotes=\"%s{escapedPackageReleasesNotes}\""
                    |> CmdLine.toString
                )
                |> Async.AwaitTask
                |> Async.RunSynchronously

            let m =
                Regex.Match(
                    standardOutput,
                    "Successfully created package '(?'nupkgPath'.*\.nupkg)'"
                )

            if not m.Success then
                failwith $"Failed to find nupkg path in output:\n{standardOutput}"

            Nuget.push (
                m.Groups.["nupkgPath"].Value,
                Environment.GetEnvironmentVariable("NUGET_KEY")
            )

            Command.Run("git", "add .")

            Command.Run(
                "git",
                CmdLine.empty
                |> CmdLine.appendRaw "commit"
                |> CmdLine.appendPrefix "-m" $"chore: release {newVersion.ToString()}"
                |> CmdLine.toString
            )

            Command.Run("git", "push")

            0
