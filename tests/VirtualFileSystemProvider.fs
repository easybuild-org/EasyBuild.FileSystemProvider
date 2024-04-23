module Tests.VirtualFileSystemProvider

open Expecto
open Tests.Utils
open EasyBuild.FileSystemProvider
open EasyBuild.FileSystemProvider.VirtualFileSystemProvider
open System.IO

type VirtualWorkspaceRelativeToCurrentDir =
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

type VirtualWorkspaceRoot = VirtualFileSystem<"../src", "dist">

let tests =
    testList
        "VirtualFileSystemProvider"
        [
            testList
                "Parser"
                [
                    test "works with empty config" {
                        let config = ""

                        let expected = Parser.INode(".", -1)

                        let actual = Parser.parse "." config

                        Expect.equal actual expected
                    }

                    test "works with mutiple root folders" {
                        let config =
                            """
dist
    client
        index.html
        app.js
docs
public
    style.css
                        """

                        let expected = Parser.INode(".", -1)

                        let dist = Parser.INode("dist", 0, expected)
                        expected.Children.Add(dist)
                        let client = Parser.INode("client", 4, dist)
                        dist.Children.Add(client)
                        client.Children.Add(Parser.INode("index.html", 8, client))
                        client.Children.Add(Parser.INode("app.js", 8, client))

                        expected.Children.Add(Parser.INode("docs", 0, expected))

                        let public' = Parser.INode("public", 0, expected)
                        public'.Children.Add(Parser.INode("style.css", 4, public'))
                        expected.Children.Add(public')

                        let actual = Parser.parse "." config

                        Expect.equal actual expected
                    }

                    test "name with trailing slash is a folder" {
                        let config =
                            """
dist/
                        """

                        let actual = Parser.parse "." config

                        Expect.equal actual.Children.[0].IsFolder true
                    }
                ]

            testList
                "TypeProvider"
                [
                    test "allows to access file path directly" {
                        let expected =
                            Path.GetFullPath(
                                Path.Join(__SOURCE_DIRECTORY__, "dist", "client", "index.html")
                            )

                        Expect.equal
                            VirtualWorkspaceRelativeToCurrentDir.dist.client.``index.html``
                            expected
                    }

                    test "allows access to empty directory path" {
                        let expected = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "docs"))

                        Expect.equal VirtualWorkspaceRelativeToCurrentDir.docs.``.`` expected
                    }

                    test "going down, up, and down again works" {
                        let expected = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "dist"))

                        Expect.equal
                            VirtualWorkspaceRelativeToCurrentDir.dist.``..``.dist.``.``
                            expected
                    }

                    test "going down, down, up, up, and down again works" {
                        let expected = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "dist"))

                        Expect.equal
                            VirtualWorkspaceRelativeToCurrentDir.dist.client.``..``.``..``.dist.``.``
                            expected
                    }

                    test "Directory path can be accessed using ToString()" {
                        let distExpected = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "dist"))

                        let publicExpected =
                            Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "public"))

                        Expect.equal
                            (VirtualWorkspaceRelativeToCurrentDir.dist.ToString())
                            distExpected

                        Expect.equal
                            (VirtualWorkspaceRelativeToCurrentDir.dist.``..``.``public``.ToString())
                            publicExpected
                    }
                ]
        ]
