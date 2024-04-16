module EasyBuild.FileSystemProvider.VirtualFileSystemProvider

open System.Reflection
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System
open System.IO
open System.Text.RegularExpressions

module Parser =

    // Tree parser is based on the work of Nathan Friend:
    // https://gitlab.com/nfriend/tree-online/-/blob/ef0414eb6f5f097d38272ef45b6633b2d7aaec62/src/lib/parse-input.ts

    type INode(name: string, indentCount: int, ?parent: INode) =
        let children = ResizeArray<INode>()
        let mutable parent = parent

        /// <summary>
        /// Returns the name of the inode
        /// </summary>
        /// <returns></returns>
        member _.Name =
            // Remove trailing slash or backslash
            // The name of a folder should not have a trailing slash
            name.TrimEnd('/', '\\')

        /// <summary>
        /// Returns a normalized name for the inode.
        ///
        /// If the inode is a file, the name is returned as is.
        /// If the inode is a folder, the name is returned with a trailing slash.
        /// </summary>
        /// <returns></returns>
        member this.NormalizedName =
            if this.IsFolder then
                this.Name + "/"
            else
                this.Name

        member _.IndentCount = indentCount
        member _.Children = children
        member _.Parent = parent

        member _.SetParent(parentRef: INode) = parent <- Some parentRef

        /// <summary>
        /// Get a value indicating whether the inode is a file or not.
        ///
        /// By opposition, to a folder a file is a node without children and without a trailing slash or backslash.
        /// </summary>
        /// <returns>
        /// True if the inode is a file, false otherwise.
        /// </returns>
        member this.IsFile = not this.IsFolder

        /// <summary>
        /// Get a value indicating whether the inode is a folder or not.
        ///
        /// A folder is a node with children or a node with a trailing slash or backslash.
        /// </summary>
        /// <returns>
        /// True if the inode is a folder, false otherwise.
        /// </returns>
        member _.IsFolder =
            // A folder is a node with children or a node with a trailing slash
            // Trailing slash allows to have empty folders
            children.Count > 0 || name.EndsWith("/") || name.EndsWith("\\")

        override this.Equals(obj) =
            match obj with
            | :? INode as other ->
                let parentLiteEquals =
                    match this.Parent, other.Parent with
                    | Some thisParent, Some otherParent ->
                        // We can't use the default equals because it will cause a stack overflow
                        // because the types have mutual references
                        thisParent.Name = otherParent.Name
                        && thisParent.IndentCount = otherParent.IndentCount
                        && thisParent.Children.Count = otherParent.Children.Count
                    | None, None -> true
                    | _ -> false

                let childrenEquals =
                    this.Children.Count = other.Children.Count
                    && Seq.forall2
                        (fun thisChild otherChild -> thisChild.Equals(otherChild))
                        this.Children
                        other.Children

                other.Name = this.Name
                && other.IndentCount = this.IndentCount
                && parentLiteEquals
                && childrenEquals

            | _ -> false

        override this.GetHashCode() =
            // Is it the correct way to implement GetHashCode?
            // It was proposed by Copilot and because I never implemented GetHashCode
            // I am trusting it, nothing bad can happen, right? :)
            HashCode.Combine(this.Name, this.IndentCount, this.Parent, this.Children)

    let private processText (text: string) =
        // Normalize line endings
        text.Replace("\r\n", "\n").Split('\n')
        // Remove empty lines
        |> Array.filter (String.IsNullOrWhiteSpace >> not)
        // Transform into a inode
        |> Array.map (fun line ->
            let regex = Regex("^(?'indentation'\s*)(?'name'.*)$")

            let m = regex.Match(line)

            if m.Success then
                let indentation = m.Groups.["indentation"].Value.Length
                let name = m.Groups.["name"].Value

                INode(name, indentation)
            else
                failwithf "Failed to parse line: %s" line
        )

    /// <summary>
    /// Parse a configuration text into a INode tree.
    /// </summary>
    /// <param name="rootFolder">
    /// Name of the root inode
    ///
    /// It can be a simple name like "dist", "." or a path like "dist/client" or "/home/user/"
    /// </param>
    /// <param name="config">Config to parse</param>
    /// <returns>Returns a INode representing the parser configuration</returns>
    let rec parse (rootFolder: string) (config: string) =
        let root = INode(rootFolder, -1)
        let inodes = processText config

        let mutable walkingPaths = ResizeArray [ root ]

        for inode in inodes do
            while walkingPaths[walkingPaths.Count - 1].IndentCount >= inode.IndentCount do
                // Pop the last element
                walkingPaths.RemoveAt(walkingPaths.Count - 1)

            let parent = walkingPaths[walkingPaths.Count - 1]
            parent.Children.Add(inode)
            inode.SetParent parent

            walkingPaths.Add(inode)

        root

let private createFileLiteral
    (inode: Parser.INode)
    (rootPath: string)
    (rootType: ProvidedTypeDefinition)
    =

    let fullPath = rootPath + "/" + inode.Name
    let pathFieldLiteral = ProvidedField.Literal(inode.Name, typeof<string>, fullPath)

    pathFieldLiteral.AddXmlDoc $"Path to '{fullPath}'"

    rootType.AddMember pathFieldLiteral

let rec private createInodeProperties
    (inode: Parser.INode)
    (rootPath: string)
    (rootType: ProvidedTypeDefinition)
    =

    let directoryInfo = Path.Combine(rootPath, inode.Name) |> DirectoryInfo

    // If we are not at the top of the virtual tree, we add access to the current folder
    // If user needs to access the current folder, he should use RelativeFileSystemProvider instead
    if inode.IndentCount <> -1 then
        let currentFolderField =
            ProvidedField.Literal(".", typeof<string>, directoryInfo.FullName)

        rootType.AddMember currentFolderField

    // Add parent directory if we have one
    match inode.Parent with
    | Some parent ->
        rootType.AddMemberDelayed(fun () ->
            let directoryType =
                ProvidedTypeDefinition("..", Some typeof<obj>, hideObjectMethods = true)

            createInodeProperties parent directoryInfo.Parent.FullName directoryType
            directoryType
        )

    | None -> ()

    inode.Children
    |> Seq.iter (fun inode ->
        if inode.IsFile then
            createFileLiteral inode directoryInfo.FullName rootType
        else
            let folderType =
                ProvidedTypeDefinition(inode.Name, Some typeof<obj>, hideObjectMethods = true)

            folderType.AddXmlDoc $"Interface representing folder '{directoryInfo.FullName}'"

            createInodeProperties inode directoryInfo.FullName folderType
            rootType.AddMember folderType
    )

[<TypeProvider>]
type VirtualFileSystemProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let namespaceName = "EasyBuild.FileSystemProvider"
    let assembly = Assembly.GetExecutingAssembly()

    let relativeFileSystem =
        ProvidedTypeDefinition(
            assembly,
            namespaceName,
            "VirtualFileSystem",
            Some typeof<obj>,
            hideObjectMethods = true
        )

    do
        relativeFileSystem.DefineStaticParameters(
            parameters =
                [
                    ProvidedStaticParameter("relativePath", typeof<string>)
                    ProvidedStaticParameter("configText", typeof<string>)
                ],
            instantiationFunction =
                fun typeName parametersValue ->
                    let relativePath = parametersValue.[0] :?> string
                    let configText = parametersValue.[1] :?> string

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

                        // Note: We allow to provide the top level root path, because if we use "."
                        // this cause issues when trying to navigate downwards/upwards several levels
                        let rootInode = Parser.parse rootDirectory.FullName configText

                        if rootInode.Children.Count = 0 then
                            failwith
                                "The configuration seems to be empty, please provide a valid configuration."

                        createInodeProperties rootInode rootDirectory.FullName rootType

                        rootType
        )

    do this.AddNamespace(namespaceName, [ relativeFileSystem ])

[<assembly: TypeProviderAssembly>]
do ()
