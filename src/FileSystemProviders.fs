﻿module EasyBuild.FileSystemProvider.FileSystemProviders

open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices

open System.IO

let private createFileLiterals
    (directoryInfo: DirectoryInfo)
    (rootType: ProvidedTypeDefinition)
    (makePath: string -> string)
    =

    for file in directoryInfo.EnumerateFiles() do
        let pathFieldLiteral =
            ProvidedField.Literal(file.Name, typeof<string>, file.FullName |> makePath)

        pathFieldLiteral.AddXmlDoc $"Path to '{file.FullName}'"

        rootType.AddMember pathFieldLiteral

let rec private createDirectoryProperties
    (directoryInfo: DirectoryInfo)
    (rootType: ProvidedTypeDefinition)
    (makePath: string -> string)
    =

    // Extract the full path in a variable so we can use it in the ToString method
    let currentFolderFullName = directoryInfo.FullName |> makePath

    let currentFolderField =
        ProvidedField.Literal(".", typeof<string>, currentFolderFullName)

    let toStringMethod =
        ProvidedMethod(
            "ToString",
            [],
            typeof<string>,
            isStatic = true,
            invokeCode = fun args -> <@@ currentFolderFullName @@>
        )

    let xmlDocText = $"Get the full path to '{currentFolderFullName}'"

    currentFolderField.AddXmlDoc xmlDocText
    toStringMethod.AddXmlDoc xmlDocText

    rootType.AddMember currentFolderField
    rootType.AddMember toStringMethod

    // Provide pregenerated access to DirectoryInfo to provide access to other
    // helpers without requiring us to construct them all in the type provider
    let getDirectoryInfo =
        let currentFolderFullName = <@@ currentFolderFullName @@>

        let method =
            ProvidedMethod(
                "GetDirectoryInfo",
                [],
                typeof<DirectoryInfo>,
                isStatic = true,
                invokeCode =
                    fun args -> <@@ let dir = DirectoryInfo(%%currentFolderFullName) in dir @@>
            )

        method.AddXmlDoc(
            """<summary>Retrieves the <c>DirectoryInfo</c> object for the current directory.</summary>
<returns>The current directory <c>DirectoryInfo</c> object"""
        )

        method

    rootType.AddMember getDirectoryInfo
    createFileLiterals directoryInfo rootType makePath

    // Add parent directory
    rootType.AddMemberDelayed(fun () ->
        let directoryType =
            ProvidedTypeDefinition("..", Some typeof<obj>, hideObjectMethods = true)

        directoryType.AddXmlDoc $"Interface representing directory '{directoryInfo.FullName}'"

        createDirectoryProperties directoryInfo.Parent directoryType makePath
        directoryType
    )

    for folder in directoryInfo.EnumerateDirectories() do
        // Build the folder member on demand as we can have a lot of folders/files
        rootType.AddMemberDelayed(fun () ->
            let folderType =
                ProvidedTypeDefinition(folder.Name, Some typeof<obj>, hideObjectMethods = true)

            folderType.AddXmlDoc $"Interface representing folder '{folder.FullName}'"

            // Walk through the folder
            createDirectoryProperties folder folderType makePath
            // Store the folder type in the member
            folderType
        )

let private watchDir (directoryInfo: DirectoryInfo) =
    let watcher = new FileSystemWatcher(directoryInfo.FullName)
    watcher.EnableRaisingEvents <- true

    watcher

[<Interface>]
[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type IFileSystemProvider =
    abstract ImplementationName: string
    abstract MakePath: basePath: DirectoryInfo -> targetPath: string -> string

[<System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>]
type FileSystemProviderImpl(config: TypeProviderConfig, implementation: IFileSystemProvider) as this
    =
    inherit TypeProviderForNamespaces(config)
    let namespaceName = "EasyBuild.FileSystemProvider"
    let assembly = Assembly.GetExecutingAssembly()
    let providerName = implementation.ImplementationName
    let makePath = implementation.MakePath

    let relativeFileSystem =
        ProvidedTypeDefinition(
            assembly,
            namespaceName,
            providerName,
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

                        rootType.AddXmlDoc
                            $"Interface representing directory '{rootDirectory.FullName}'"

                        createDirectoryProperties rootDirectory rootType (rootDirectory |> makePath)

                        rootType
        )

    do this.AddNamespace(namespaceName, [ relativeFileSystem ])

[<TypeProvider>]
type AbsoluteFileSystemProvider(config: TypeProviderConfig) =
    inherit
        FileSystemProviderImpl(
            config,
            { new IFileSystemProvider with
                member this.ImplementationName = "AbsoluteFileSystem"
                member this.MakePath basePath filePath = filePath
            }
        )

[<TypeProvider>]
type RelativeFileSystemProvider(config: TypeProviderConfig) =
    inherit
        FileSystemProviderImpl(
            config,
            { new IFileSystemProvider with
                member this.ImplementationName = "RelativeFileSystem"

                member this.MakePath basePath filePath =
                    Path.getRelativePath basePath.FullName filePath
            }
        )

[<assembly: TypeProviderAssembly>]
do ()
