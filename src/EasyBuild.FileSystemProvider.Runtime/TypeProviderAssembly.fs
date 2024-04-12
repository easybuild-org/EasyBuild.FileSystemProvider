namespace global

open FSharp.Core.CompilerServices

// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly: TypeProviderAssembly("EasyBuild.FileSystemProvider.DesignTime")>]
do ()
