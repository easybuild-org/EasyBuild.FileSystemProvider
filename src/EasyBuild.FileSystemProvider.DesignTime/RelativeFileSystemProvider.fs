module EasyBuild.FileSystemProvider

open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices

open System.IO

let private createFileLiterals (directoryInfo: DirectoryInfo) (rootType: ProvidedTypeDefinition) =

    for file in directoryInfo.EnumerateFiles() do
        let pathFieldLiteral =
            ProvidedField.Literal(file.Name, typeof<string>, file.FullName)

        pathFieldLiteral.AddXmlDoc $"Path to '{file.FullName}'"

        rootType.AddMember pathFieldLiteral

let rec private createDirectoryProperties
    (directoryInfo: DirectoryInfo)
    (rootType: ProvidedTypeDefinition)
    =

    let currentFolderField =
        ProvidedField.Literal(".", typeof<string>, directoryInfo.FullName)

    currentFolderField.AddXmlDoc $"Get the full path to '{directoryInfo.FullName}'"

    rootType.AddMember currentFolderField
    createFileLiterals directoryInfo rootType

    // Add parent directory
    rootType.AddMemberDelayed( fun () ->
        let directoryType =
            ProvidedTypeDefinition(
                "..",
                Some typeof<obj>,
                hideObjectMethods = true
            )
        directoryType.AddXmlDoc $"Interface representing directory '{directoryInfo.FullName}'"

        createDirectoryProperties directoryInfo.Parent directoryType
        directoryType
    )

    for folder in directoryInfo.EnumerateDirectories() do
        // Build the folder member on demand as we can have a lot of folders/files
        rootType.AddMemberDelayed( fun () ->
            let folderType =
                ProvidedTypeDefinition(
                    folder.Name,
                    Some typeof<obj>,
                    hideObjectMethods = true
                )
            folderType.AddXmlDoc $"Interface representing folder '{folder.FullName}'"

            // Walk through the folder
            createDirectoryProperties folder folderType
            // Store the folder type in the member
            folderType
        )

[<TypeProvider>]
type RelativeFileSystemProvider(config: TypeProviderConfig) as this =
    inherit
        TypeProviderForNamespaces(
            config,
            assemblyReplacementMap =
                [ ("EasyBuild.FileSystemProvider.DesignTime", "EasyBuild.FileSystemProvider") ],
            addDefaultProbingLocation = true
        )

    let namespaceName = "EasyBuild.FileSystemProvider"
    let assembly = Assembly.GetExecutingAssembly()

    let relativeFileSystem =
        ProvidedTypeDefinition(
            assembly,
            namespaceName,
            "RelativeFileSystem",
            Some typeof<obj>,
            hideObjectMethods = true
        )

    do
        relativeFileSystem.DefineStaticParameters(
            parameters = [ ProvidedStaticParameter("relativePath", typeof<string>) ],
            instantiationFunction =
                fun typeName parametersValue ->
                    let relativePath = parametersValue.[0] :?> string

                    let rootDirectory =
                        match relativePath with
                        | ""
                        | "." -> Path.GetFullPath(config.ResolutionFolder)
                        | _ -> Path.Combine(config.ResolutionFolder, relativePath)
                        |> DirectoryInfo

                    if not rootDirectory.Exists then
                        failwith $"Directory {rootDirectory.FullName} does not exist."
                    else
                        let rootType =
                            ProvidedTypeDefinition(
                                assembly,
                                namespaceName,
                                typeName,
                                Some typeof<obj>,
                                hideObjectMethods = true
                            )

                        rootType.AddXmlDoc $"Interface representing directory '{rootDirectory.FullName}'"

                        createDirectoryProperties rootDirectory rootType

                        rootType
        )

    do this.AddNamespace(namespaceName, [ relativeFileSystem ])
