module Tests.RelativeFileSystemProvider

open Expecto
open Tests.Utils
open EasyBuild.FileSystemProvider
open System.IO

type CurrentDirectoryEmptyString = RelativeFileSystem<"">
type CurrentDirectoryDot = RelativeFileSystem<".">

type ParentDirectory = RelativeFileSystem<"..">

let getRelativePathFor value =
    Path.GetRelativePath(__SOURCE_DIRECTORY__, value)

let tests =
    testList
        "RelativeFileSystemProvider"
        [
            test "Empty string is mapped to the current directory" {
                let expected = getRelativePathFor __SOURCE_DIRECTORY__
                Expect.equal CurrentDirectoryEmptyString.``.`` expected
            }

            test "Dot is mapped to the current directory" {
                let expected = getRelativePathFor __SOURCE_DIRECTORY__
                Expect.equal CurrentDirectoryDot.``.`` expected
            }

            test "Double dot is mapped to the parent directory" {
                let expected' = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, ".."))
                let expected = Path.GetRelativePath(expected', expected')
                Expect.equal ParentDirectory.``.`` expected
            }

            test "We can navigate the tree upwards" {
                let expected =
                    getRelativePathFor
                    <| Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "..", "README.md"))

                Expect.equal CurrentDirectoryDot.``..``.src.``..``.``README.md`` expected
            }

            test "We can navigate the tree downwards" {
                let expected =
                    Path.GetFullPath(
                        Path.Join(__SOURCE_DIRECTORY__, "fixtures", "folder1", "test.txt")
                    )
                    |> getRelativePathFor

                Expect.equal CurrentDirectoryDot.fixtures.folder1.``test.txt`` expected
            }

            test "Directory path can be accessed using ToString()" {
                let expectedRoot = getRelativePathFor __SOURCE_DIRECTORY__

                let expectedFolder1 =
                    Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "fixtures", "folder1"))
                    |> getRelativePathFor

                Expect.equal (CurrentDirectoryDot.ToString()) expectedRoot
                Expect.equal (CurrentDirectoryDot.fixtures.folder1.ToString()) expectedFolder1
            }
        ]
