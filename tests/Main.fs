module Tests.Main

open Expecto

[<Tests>]
let tests =
    testList
        "All"
        [
            RelativeFileSystemProvider.tests
            VirtualFileSystemProvider.tests
            AbsoluteFileSystemProvider.tests
        ]
