# Building and maintaining the macOS client

Build on macOS with Python 3.12 or later, a Swift 6 or later compiler (such as
Xcode 16+ Command Line Tools), and zstd. The build host must support the selected
toolchain. The application itself targets macOS 13 or later, with a universal
launcher (`arm64` and `x86_64`) and the verified upstream MDPro3 macOS runtime.
A Unity Editor installation is not required.

Check `python3 --version` and `xcrun swiftc --version` before starting. Install
zstd through your package manager (`brew install zstd` for Homebrew). Downloads
use an installed libzstd when available, otherwise the `zstd` command on PATH
or at either standard Apple Silicon/Intel Homebrew location. Preparation checks
safe archive extraction before requesting publisher packages, and a complete
build checks the Swift compiler before compilation or staging.

```sh
python3 prepare-dependencies.py
python3 build.py --open
```

Dependency preparation downloads the pinned toolchain and publisher resource
inventory. Rerun the same preparation command after an interruption to resume.
Existing resource files are verified, and missing or damaged files are repaired;
this also applies to an incomplete imported runtime. `--check` verifies without
downloading or repairing files. Download progress and diagnostic reports remain
local and are ignored by Git.

To reuse an existing checkout without modifying it:

```sh
python3 prepare-dependencies.py --from /path/to/existing/checkout
python3 prepare-dependencies.py --check
```

Interrupted dependency copies are recovered on the next `--from` run. A copy
lock prevents another preparation process from replacing active staging files;
its local lock file is retained so retries share the same lock.

The importer copies top-level dependency links into independent directories and
preserves internal links. If your existing `vendor/MDPro3-mac` or its workspace
parent directory is a symbolic link, `--check` may verify it, but preparation
refuses to repair through the link. New dependency copies also refuse linked
destination paths under `vendor/`, `.tools/`, or `build/`. Move the reported
link to a backup location, then rerun `--from` with the donor checkout to create
a separate runtime copy. The importer never removes the link
for you. Full builds copy the actual runtime directory into independent staging;
publisher files containing internal links are rejected before pruning or seeding.

The application is written to `dist/Yu-Gi-Oh! Fight.app`. Toolchains, publisher
runtime downloads, intermediate generations, applications and diagnostic
reports are ignored by Git. The game keeps user data separately in
`~/Library/Application Support/Yu-Gi-Oh! Fight/`.

## Source and resource verification

`python3 build.py --code-only` compiles C# sources and verifies their external
members against the matching publisher assemblies. The compiler embeds the
pinned tiktoken rank table and binds client artwork and academy files to their
SHA-256 hashes. `tools/source-toolchain.json` records the .NET and Mono.Cecil
versions; `vendor/MDPro3-release-source/manifest.json` records source provenance.

A complete build validates publisher checksums, academy cards and replays,
the source assembly in the publisher's Mono runtime, the separate native
server host, the universal native file selector and the final macOS code
signature. Packaging publishes the new
application atomically. Close the game before replacing its application.

Source edits made during compilation or packaging require a fresh build. The
compiler rechecks its C# file inventory, and publication rechecks the current
sources, client content, documentation, licenses and native/launcher inputs.
Input changes detected in a source build keep the prior source pointer;
changes detected before application publication keep the prior application
rather than installing a stale build.

After a code-only change, an existing application can be updated with
`python3 scripts/update-mdpro3-code.py`. It refuses stale source inputs,
changed resources, native bridges or launcher changes; these require a complete build.

Local test scripts and acceptance fixtures are intentionally excluded from
Git. `--ui-proof` is available only for code-only local validation builds;
such assemblies cannot be installed as release updates or packaged as release
applications. Production builds and their checks do not require those local
test files. Academy authoring provenance can additionally be checked with
`python3 scripts/validate-deck-academy.py --authoring-sources` when the local
recording tools are present.

## Optional archive

```sh
python3 scripts/package-client-release.py
```

This verifies the release manifest, assembly, notices and application
signature, then writes a CRC-checked ZIP64 archive and `SHA256SUMS.txt` under
`dist/`. It does not publish or upload files. Third-party runtime and asset
rights remain subject to [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

## 中文说明

在支持所选工具链的 macOS 上使用 Python 3.12+、Swift 6+ 编译器（例如 Xcode 16+
Command Line Tools）与 zstd 构建。先检查 `python3 --version` 和
`xcrun swiftc --version`；Homebrew 用户可执行 `brew install zstd`。
应用本身支持 macOS 13+。执行上方依赖准备与构建命令即可生成应用，无需安装
Unity Editor。运行数据存放在独立的用户目录，
不进入仓库或安装包。构建工具验证固定发行资源、源码兼容性、卡组学院录像、
Mono 加载、双架构原生文件选择器和应用签名；本地验收脚本与产物不提交 Git。

依赖准备中断后，重复相同命令即可续传；缺失或损坏的文件会自动修复，包括从
其他目录导入的不完整运行时。`--check` 只验证，不下载或修复；下载进度与
诊断报告仅保留在本地，不提交 Git。

`--from` 拷贝中断后也可直接重试；进程锁保护正在拷贝的临时目录，锁文件保留
在本地供重试共用。发行包下载前检查安全解包依赖，完整构建在编译前检查 Swift 版本。

导入会将顶层依赖链接复制为独立目录，并保留内部链接。已有 `vendor/MDPro3-mac`
或其工作区内父目录若是符号链接，可用 `--check` 只读验证；准备命令不会沿链接
修复来源目录。新依赖拷贝也会拒绝 `vendor/`、`.tools/` 或 `build/` 下经过链接的
目标路径。请先将链接移到备份位置，再对来源工作区运行 `--from` 以建立独立副本，工具不会
自行删除链接。完整构建复制真实运行时目录，发行文件中的内部链接会在整理或
播种前被拒绝。

仅修改 C# 代码时可先 `--code-only`，再执行代码更新脚本。资源或启动器修改需
完整构建；原生桥接修改同样需要完整构建。更新与构建前请关闭游戏。归档命令只生成本地 ZIP，不执行上传。

构建期间修改源码或资源时需要重新构建。编译与发布前会重新核对 C# 文件清单，
应用发布前也核对客户端内容、文档、许可证及原生组件和启动器输入；源码构建
检测到输入变化时保留原有源码指针，应用发布前检测到变化时保留原有应用，
避免安装过时版本。
