# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!-- EasyBuild: START -->
<!-- last_commit_released: b2f46154764784c286f7f3fba8479dd3bea33a4d -->
<!-- EasyBuild: END -->

## 1.1.0

### 🚀 Features

- Add `GetDirectoryInfo()` for easier access to `DirectoryInfo` type (#15) ([b2f4615](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/b2f46154764784c286f7f3fba8479dd3bea33a4d))
### 🐞 Bug Fixes

- Target netstandard2.0 allowing back usage from VS (#16) ([15ce1f9](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/15ce1f9821773305551fb0cc5f87297cb23b1660))

## 1.0.0

### 🚀 Features

- Add `AbsoluteFileSystem` and `RelativeFileSystem` (#14) ([287fbf3](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/287fbf31e0b0c9e4d45dfbcfdbae83c6b9314542))

## 0.3.1

### 🐞 Bug Fixes

- VirtualFileSystem uses platform dependent separator (#12) ([36d90ab](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/36d90abf0bb2546be0da6d6157242dd747569ec3))

## 0.3.0

### 🚀 Features

- Add `ToString()` as a static member to directories interfaces + add XmlDoc to dot inode for VirtualFileSystem ([31c3019](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/31c3019c1572542c4615bae8fb2757175efacaa3))

## 0.2.0

### 🐞 Bug Fixes

- Target netstandard2.0 trying to fix VS support ([866b362](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/866b3620ca92ef200caef706d73db047ea98956a))

## 0.1.1

### 🐞 Bug Fixes

- Use inlined version of TPSDK files instead of the NuGet package (could not make the NuGet SDK version work) ([c4548a5](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/c4548a5433a2732aad9f9d5ca090bdcc60bd517c))

## 0.1.0

### 🚀 Features

- Add `VirtualFileSystemProvider` ([cd07530](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/cd075303effc4b43838f4effed5860a6e0bfca6f))
- Initial implementation of `RelativeFileSystemProvider` ([7e24b36](https://github.com/easybuild-org/EasyBuild.FileSystemProvider/commit/7e24b3670733dd6abf272c8d5fe1f7a68ac91d56))
