# Building and maintaining the macOS client

The supported build host is macOS 13 or later with Xcode Command Line Tools
and Python 3. The launcher is universal (`arm64` and `x86_64`); the game runtime
is the verified upstream MDPro3 macOS release. A Unity Editor installation is
not required for this source build.

```sh
python3 prepare-dependencies.py
python3 build.py --open
```

Dependency preparation downloads the pinned toolchain and publisher resource
inventory. To reuse an existing checkout without modifying it:

```sh
python3 prepare-dependencies.py --from /path/to/existing/checkout
python3 prepare-dependencies.py --check
```

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
server host and the final macOS code signature. Packaging publishes the new
application atomically. Close the game before replacing its application.

After a code-only change, an existing application can be updated with
`python3 scripts/update-mdpro3-code.py`. It refuses stale source inputs,
changed resources or launcher changes; these require a complete build.

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

使用 macOS 13+、Xcode Command Line Tools 和 Python 3 构建。执行上方依赖准备与
构建命令即可生成应用，无需安装 Unity Editor。运行数据存放在独立的用户目录，
不进入仓库或安装包。构建工具验证固定发行资源、源码兼容性、卡组学院录像、
Mono 加载和应用签名；本地验收脚本与产物不提交 Git。

仅修改 C# 代码时可先 `--code-only`，再执行代码更新脚本。资源或启动器修改需
完整构建。更新与构建前请关闭游戏。归档命令只生成本地 ZIP，不执行上传。
