# 发行版 Addressables 依赖诊断

`catalog.bin` 是实际 MDPro3 `darwin / 1.4.7-2` 发行文件，SHA-256 为 `5107a480cdc9e6370d4cd574e81ee105d1663e026651ba0280cca0fa61655c74`。本工具使用发行版 `ContentCatalogData` / `BinaryStorageBuffer` 读取器、发行版 Mono 与 corlib，不使用推测的二进制格式。

```sh
python3 scripts/dump-shipping-catalog.py
python3 scripts/audit-catalog-dependencies.py \
  --paths-output vendor/MDPro3-mac-metadata/catalog-core-asset-closure-paths.json \
  --expanded-baseline-output vendor/MDPro3-mac-metadata/catalog-core-expanded-baseline-paths.json
```

生成器验证目录、所有复制的 DLL 和两份 Mono 原生库的 publisher SHA-256，再编译诊断器。作者工具链固定为 `.NET 8.0.427`、`Mono.Cecil 0.11.6`，来源见 `../source-toolchain.json`。输出 `artifacts/shipping-addressables-catalog.json` 包含 16,671 keys / 16,683 locations，旁边的 `.provenance.json` 记录输入与输出哈希。

只在 `build/catalog-dump-managed` 的诊断副本中改变三处 Addressables 方法：保留原始 internal ID、跳过 ResourceManager / profiler 场景初始化、不解码图遍历未使用的 AssetBundleRequestOptions。原生桥仅提供标准 memcpy，并把未处理的 reader 异常输出后立即终止。没有模拟 Unity Object 内存布局，没有吞掉异常，没有修改发行目录或游戏 runtime。该输出是 ID、类型、provider 与依赖边的投影，**不包含 hash/CRC/cache 请求选项，不适用于替换游戏目录**。

默认审计扫描 `core-bundles.json` 中 14 个核心 bundle 对应的所有 237 个 asset locations，递归闭合到 507 个 bundle。所有 507 个发行文件已通过 publisher SHA-256；当前 4,542 路径的 baseline 包含全部依赖。早期只选择若干菜单 key 得到 504 个 bundle，余下 3 个核心文件已在 baseline 中。不能只从 bundle location 的 Dependencies 判断完整性：bundle 节点的列表常为空，asset 节点才带有 SpriteAtlas 和共享资源依赖。

查找运行时明确的加载 key 后，可单独审计：

```sh
python3 scripts/audit-catalog-dependencies.py \
  --key 'ServantUI/MainMenuUI.prefab' \
  --key 'ServantUI/SoloSelectorUI.prefab' \
  --report artifacts/catalog-main-solo-closure.json
```

key 必须与实际目录完全一致；未知 key、未解析依赖、非发行目录路径、缺失文件或哈希错误都会失败。清单覆盖序列化静态依赖；按字符串动态加载的音频、卡图、伙伴、场地、MasterDuel ABLoader 资产与演出还需要各自的实际运行验证。507 个包齐全不能证明整个游戏资源、全部卡片规则或官方一致性已经完成。
