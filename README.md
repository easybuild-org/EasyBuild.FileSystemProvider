# EasyBuild.FileSystemProvider

[![NuGet](https://img.shields.io/nuget/v/EasyBuild.FileSystemProvider.svg)](https://www.nuget.org/packages/EasyBuild.FileSystemProvider)
[![](https://img.shields.io/badge/Sponsors-EA4AAA)](https://mangelmaxime.github.io/sponsors/)

EasyBuild.FileSystemProvider is a library that provides a set of F# Type Providers to provide a typed representation of files and directories based on your project structure or a virtual file system.

## Why?

In every project of mine, I need to orchestrate tasks like building, testing, etc. which involves working with files and directories. The standard way of doing it is by using hardcoded `string` but it is easy to break. You also need to remember what is current working directory or relative path you are working with.

To fix this problem, I created this library that provides 2 F# Type Providers:

- `RelativeFileSystemProvider`, typed representation of files and directories based on your project structure.
- `VirtualFileSystemProvider`, typed representation of files and directories based on a virtual file system.

### When to use each one?

Use `RelativeFileSystemProvider` when you want to access files and directories that are tracked in your project. For example, you want to access the path of your `fsproj` file or a `public` assets folder.

Use `VirtualFileSystemProvider` when you want to access files and directories that are not tracked in your project. For example, you want to use a destination folder or access a `obj`, `bin` folder.

## Installation

```bash
dotnet add package EasyBuild.FileSystemProvider
```

## Usage

### `RelativeFileSystemProvider`

Provide a representation based on your file system structure.

```fsharp
open EasyBuild.FileSystemProvider

// Path the relative path you want to work with
// You can use `"."` or `""` to represent the current directory

type Workspace = RelativeFileSystem<".">

type SourceWorkspace = RelativeFileSystem<"./src/">
```

Each folder have 2 special properties and 1 static method:

- ` ``.`` `: Represents the current folder
- ` ``..`` `: Represents the parent folder
- `ToString()`: alias to ` ``.`` ` if you don't want to use the backtick syntax

Example:

Imagine you have the following project structure:

```text
/home/project/
├── client/
│   ├── index.html
│   └── app.js
└── docs/
```

```fsharp
// Workspace represents the root folder `/home/project/`
type Workspace = RelativeFileSystem<".">

Workspace.client.``index.html`` // gives you "/home/project/client/index.html"
Workspace.client.``.`` // gives you "/home/project/client"
Workspace.client.``..``.docs // gives you "/home/project/docs"
// etc.
```

> [!WARNING]
> At the time of writing, `RelativeFileSystemProvider` does not watch you filesystem for changes. To manually refresh changes, you can do one of the following:
> * Rebuild the project
> * Restart the IDE
>
> This is [planned](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/issues/1) to be improved in the future

### `VirtualFileSystemProvider`

Provide a representation based on a virtual file system.

```fsharp
open EasyBuild.FileSystemProvider

type VirtualWorkspace =
    VirtualFileSystem<
        ".",    // Relative path for the root folder
        """
dist
    client
        index.html
        app.js
docs/
public
    style.css
    """
     >
```

Template format:

- Empty directories are represented by a line with the directory name followed by `/`:

    ```text
    docs/
    ```

- Files are represented by a line with the file name:

    ```text
    index.html
    docs
    ```

    Here `docs` is a file, not a directory because it does not have a `/` at the end.

- Indentation is used to represent the hierarchy of the files and directories.

> [!NOTE]
> You can use any number of spaces or tabs for indentation.

    ```text
    dist
        client
            index.html
            app.js
    docs/
    public
        style.css
    ```

Example:

We consider that you are initializing the `VirtualWorkspace` at `/home/project/`.

```fsharp
// VirtualWorkspace represents the root folder `/home/project/`
type VirtualWorkspace =
    VirtualFileSystem<
        ".",
        """
dist
    client
        index.html
        app.js
docs/
public
    style.css
    """
     >

VirtualWorkspace.dist.client.``index.html`` // gives you "/home/project/dist/client/index.html"
VirtualWorkspace.dist.client.``.`` // gives you "/home/project/dist/client"
VirtualWorkspace.dist.``..``.docs.``.`` // gives you "/home/project/docs"
```

## Contributing

If you want to contribute to this project, and see errors in the `build` because of the Type Providers, it is possible that you need to build them manually once.

1.
    ```bash
    dotnet build src
    ```

2. Reload the project in your IDE
3. Everything should be fine now
