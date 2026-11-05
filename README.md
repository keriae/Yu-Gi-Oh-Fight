# Yu-Gi-Oh! Fight

基于 `../masterduel` 的本地游戏重构。菜单、卡组、学院、回放、收藏和设置使用统一的暗蓝、古金与青蓝视觉系统；对局内卡文、控制按钮、教学指导和 AI 状态栏也提供配套精修。对局规则、场地、真实卡图与原生操作继续使用 MDPro3 实现。设计图用于风格参考，实际卡库、教程和对局数据来自原项目。

## 构建与启动

macOS 13+，Apple Silicon / Intel。开发构建需要 Xcode Command Line Tools 和 Python 3，工具脚本会准备固定 .NET 8 / Mono.Cecil 依赖。游戏安装包包含运行时，运行不需要 Unity Editor 或 .NET。

```sh
python3 prepare-dependencies.py --from ../masterduel
python3 build.py --open
```

也可省略 `--from`，按原项目的固定发行清单下载所需资源。`--from` 只读取原项目，将缺失的运行时和工具链克隆到本项目，已经存在的目录不替换。检查依赖用 `python3 prepare-dependencies.py --check`。

应用输出为 `dist/Yu-Gi-Oh! Fight.app`。只编译 C# 用 `python3 build.py --code-only`。完整构建保留原发行 SHA-256、程序集兼容检查、原生规则和真实 Mono 加载验证，最后签名并原子发布应用。

已有成功构建时，可在编译后运行 `python3 scripts/update-mdpro3-code.py` 更新应用中的程序集并重新签名。该命令校验源码、资源和旧应用清单；修改资源、启动器或原生服务后应执行完整构建。

原生 UI 验收使用 `python3 build.py --code-only --ui-proof`，测试代码集中在 `Client/Validation/`。普通构建排除验收入口与布局调试代码，更新器拒绝验收程序集。最终验收、交付核对和中间产物清理命令见 [最终交付记录](docs/CLIENT_UX_REFINEMENT.md)。最终应用为 `dist/Yu-Gi-Oh! Fight.app`，直接启动即可；压缩分享是可选操作，大型运行时和构建产物不提交 Git。

## 存档与迁移

新版使用 `~/Library/Application Support/Yu-Gi-Oh! Fight/`，不会自动改动旧版 `MDPro3 Offline` 存档。旧卡组、录像、残局、自定义图片和常用设置可显式导入：

```sh
python3 scripts/import-legacy-state.py
python3 scripts/import-legacy-state.py --apply
```

第一条仅预览。第二条复制缺失的个人文件，遇到同名文件保留新版内容，并保存 `Legacy-Import.json` 清单。首次启动前导入可同时带入旧设置；启动后的同名设置仍保留新版值。请关闭两版游戏后导入。AI 密钥默认不导入；如需复制已记住的密钥，加 `--include-credentials`，输出不会显示密钥内容。

## 源码与资源

- `game/Assets/Scripts/MDPro3/`：游戏源码及新版客户端 UI；Unity 绑定类型和字段保留兼容性。
- `content/client-ui/`：新版主视觉、探索插画及独立品牌字标，构建时安装到 `Client/Data/ClientUI/`，每个文件写入打包哈希清单。素材使用 gpt-image-2 生成，提示词及生成方法见 [素材说明](docs/CLIENT_ART.md)。
- `content/deck-academy/`：推荐卡组、52 套教程、固定起手练习和真实展开录像。
- `scripts/`、`tools/`、`native/`、`Sources/`：继承的源码编译、原生服务、检查及 macOS 启动器。
- `vendor/MDPro3-release-source/`：固定上游源码归档和引用包；大型发行运行时、工具链、构建缓存不提交 Git。

页面清单见 `docs/ui-design/PAGE_INDEX.md`，本轮跨页面与对局辅助界面的视觉、交互增量见 [客户端 UX/UI 精修](docs/CLIENT_UX_REFINEMENT.md)。新增界面及进入、返回流程的实机验证与底层构建验证分别记录；继承原项目的检查不等于新版所有页面和规则组合都已验证。详见 [迁移说明](docs/MIGRATION.md)、[验证记录](docs/VERIFICATION.md)、[逐页精修审查](docs/UI_POLISH_AUDIT.md) 与 [第三方来源](THIRD_PARTY_NOTICES.md)。

运行 `python3 -m unittest discover -s tests -v` 检查存档导入、资源绑定、并发构建和 AI 配置槽位隔离。这些函数测试在临时目录运行，不替代 Unity 新界面的实机检查。
