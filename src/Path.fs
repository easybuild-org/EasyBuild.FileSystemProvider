module EasyBuild.FileSystemProvider.Path
(*
A simple port of the GetRelativePath implementation from its introduction
in dotnet System.IO without the use of Span et al for compatibility with standard2.0
*)

open System
open System.IO
open System.Text

let private commonPathLength first second =
    let mutable commonChars = 0

    if String.IsNullOrEmpty second || String.IsNullOrEmpty first then
        commonChars
    else
        let lLength, rLength = first.Length, second.Length

        while (not (commonChars = lLength || commonChars = rLength)
               && first[commonChars] = second[commonChars]) do
            commonChars <- commonChars + 1

        match commonChars with
        | 0 -> commonChars
        | _ when
            commonChars = lLength
            && (commonChars = rLength || second[commonChars] = Path.DirectorySeparatorChar)
            ->
            commonChars
        | _ when commonChars = rLength && first[commonChars] = Path.DirectorySeparatorChar ->
            commonChars
        | _ ->
            while commonChars > 0 && first[commonChars - 1] <> Path.DirectorySeparatorChar do
                commonChars <- commonChars - 1

            commonChars

let internal getRelativePath relativeTo path =
    if isNull relativeTo || isNull path then
        nullArg "getRelativePath cannot compare nulls"

    let relativeTo = Path.GetFullPath(relativeTo)
    let path = Path.GetFullPath(path)

    if Path.GetPathRoot relativeTo <> Path.GetPathRoot path then
        path
    else
        let mutable commonLength = commonPathLength relativeTo path

        if commonLength = 0 then
            path
        else
            let mutable relativeToLength = relativeTo.Length

            if relativeTo[relativeToLength - 1] = Path.DirectorySeparatorChar then
                relativeToLength <- relativeToLength - 1

            let mutable pathLength = path.Length
            let pathEndsInSeparator = path[pathLength - 1] = Path.DirectorySeparatorChar

            if pathEndsInSeparator then
                pathLength <- pathLength - 1

            if relativeToLength = pathLength && (commonLength >= relativeToLength) then
                "."
            else
                let sb = StringBuilder(max pathLength relativeToLength, 260)

                if commonLength < relativeToLength then
                    sb.Append("..") |> ignore

                    for i in [ (commonLength + 1) .. relativeToLength - 1 ] do
                        if relativeTo[i] = Path.DirectorySeparatorChar then
                            sb.Append(Path.DirectorySeparatorChar).Append("..") |> ignore
                elif path[commonLength] = Path.DirectorySeparatorChar then
                    commonLength <- commonLength + 1

                let differenceLength =
                    pathLength - commonLength
                    + if pathEndsInSeparator then
                          1
                      else
                          0

                if differenceLength > 0 then
                    if sb.Length > 0 then
                        sb.Append(Path.DirectorySeparatorChar) |> ignore

                    sb.Append(path.Substring(commonLength, differenceLength)) |> ignore

                sb.ToString()
