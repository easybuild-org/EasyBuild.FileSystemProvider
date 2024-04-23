module EasyBuild.FileSystemProvider.RelativeFileSystemProvider

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

    let currentFolderFullName = directoryInfo.FullName

    // The type provider 'EasyBuild.FileSystemProvider.RelativeFileSystemProvider+RelativeFileSystemProvider' reported an error in the context of provided type 'EasyBuild.FileSystemProvider.RelativeFileSystem,relativePath="."', member 'ToString'. The error: Unsupported constant type 'System.IO.DirectoryInfo'. Quotations provided by type providers can only contain simple constants. The implementation of the type provider may need to be adjusted by moving a value declared outside a provided quotation literal to be a 'let' binding inside the quotation literal.F# Compiler3022

    let toStringMethod =
        ProvidedMethod(
            "ToString",
            [],
            typeof<string>,
            isStatic = true,
            invokeCode = fun args -> <@@ currentFolderFullName @@>
        )

    rootType.AddMember currentFolderField
    rootType.AddMember toStringMethod
    createFileLiterals directoryInfo rootType

    // Add parent directory
    rootType.AddMemberDelayed(fun () ->
        let directoryType =
            ProvidedTypeDefinition("..", Some typeof<obj>, hideObjectMethods = true)

        directoryType.AddXmlDoc $"Interface representing directory '{directoryInfo.FullName}'"

        createDirectoryProperties directoryInfo.Parent directoryType
        directoryType
    )

    for folder in directoryInfo.EnumerateDirectories() do
        // Build the folder member on demand as we can have a lot of folders/files
        rootType.AddMemberDelayed(fun () ->
            let folderType =
                ProvidedTypeDefinition(folder.Name, Some typeof<obj>, hideObjectMethods = true)

            folderType.AddXmlDoc $"Interface representing folder '{folder.FullName}'"

            // Walk through the folder
            createDirectoryProperties folder folderType
            // Store the folder type in the member
            folderType
        )

let private watchDir (directoryInfo: DirectoryInfo) =
    let watcher = new FileSystemWatcher(directoryInfo.FullName)
    watcher.EnableRaisingEvents <- true

    watcher

[<TypeProvider>]
type RelativeFileSystemProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

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

                        rootType.AddXmlDoc
                            $"Interface representing directory '{rootDirectory.FullName}'"

                        createDirectoryProperties rootDirectory rootType

                        rootType
        )

    do this.AddNamespace(namespaceName, [ relativeFileSystem ])

[<assembly: TypeProviderAssembly>]
do ()
