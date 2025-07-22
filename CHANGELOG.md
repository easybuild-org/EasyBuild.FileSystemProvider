# Changelog

All notable changes to this project will be documented in this file.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!-- EasyBuild: START -->
<!-- last_commit_released: ddee4ba9bb0dc8cb22a9ca1198d93e1a3c8065e9 -->
<!-- EasyBuild: END -->

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
