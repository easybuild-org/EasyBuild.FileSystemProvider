module Tests.RelativeFileSystemProvider

open Expecto
open Tests.Utils
open EasyBuild.FileSystemProvider
open System.IO

type CurrentDirectoryEmptyString = RelativeFileSystem<"">
type CurrentDirectoryDot = RelativeFileSystem<".">
type ParentDirectory = RelativeFileSystem<"..">

let private getRelativePath value =
    Path.GetRelativePath(__SOURCE_DIRECTORY__, value)

let tests =
    testList
        "RelativeFileSystemProvider"
        [
            test "Empty string is mapped to the current directory" {
                let expected = getRelativePath __SOURCE_DIRECTORY__
                Expect.equal CurrentDirectoryEmptyString.``.`` expected
            }

            test "Dot is mapped to the current directory" {
                let expected = getRelativePath __SOURCE_DIRECTORY__
                Expect.equal CurrentDirectoryDot.``.`` expected
            }

            test "Double dot is mapped to the parent directory" {
                let expected' = Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, ".."))
                let expected = Path.GetRelativePath(expected', expected')
                Expect.equal ParentDirectory.``.`` expected
            }

            test "We can navigate the tree upwards" {
                let expected =
                    Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "..", "README.md"))
                    |> getRelativePath

                Expect.equal CurrentDirectoryDot.``..``.src.``..``.``README.md`` expected
            }

            test "We can navigate the tree downwards" {
                let expected =
                    Path.GetFullPath(
                        Path.Join(__SOURCE_DIRECTORY__, "fixtures", "folder1", "test.txt")
                    )
                    |> getRelativePath

                Expect.equal CurrentDirectoryDot.fixtures.folder1.``test.txt`` expected
            }

            test "Directory path can be accessed using ToString()" {
                let expectedRoot = getRelativePath __SOURCE_DIRECTORY__

                let expectedFolder1 =
                    Path.GetFullPath(Path.Join(__SOURCE_DIRECTORY__, "fixtures", "folder1"))
                    |> getRelativePath

                let test = CurrentDirectoryDot.ToString()

                Expect.equal (CurrentDirectoryDot.ToString()) expectedRoot
                Expect.equal (CurrentDirectoryDot.fixtures.folder1.ToString()) expectedFolder1
            }

            test "DirectoryInfo accessible from GetDirectoryInfo()" {
                let expectedWorkingDir = DirectoryInfo(System.Environment.CurrentDirectory)
                let actualWorkingDir = CurrentDirectoryDot.GetDirectoryInfo()
                Expect.equal (actualWorkingDir.FullName) expectedWorkingDir.FullName

                Expect.equal
                    (actualWorkingDir.EnumerateDirectories() |> Seq.length)
                    (expectedWorkingDir.GetDirectories() |> Seq.length)

                let expectedFixturesFolder =
                    Path.Join(System.Environment.CurrentDirectory, "fixtures") |> DirectoryInfo

                let actualFixturesFolder = CurrentDirectoryDot.fixtures.GetDirectoryInfo()

                Expect.equal (actualFixturesFolder.FullName) expectedFixturesFolder.FullName

                Expect.equal
                    (actualFixturesFolder.EnumerateDirectories() |> Seq.length)
                    (expectedFixturesFolder.GetDirectories() |> Seq.length)
            }

        ]
