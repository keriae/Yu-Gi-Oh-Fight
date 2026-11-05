# Client artwork and content

`content/client-ui/` contains the project's decorative interface artwork,
brand treatment and opening-coin illustrations. These were created with an
AI-assisted image workflow using `gpt-image-2`, then cropped, resized and
processed for transparency. UI text, buttons, focus states and interaction are
drawn by native client controls rather than baked into screenshots.

Decorative card-shaped illustrations are not playable cards and do not supply
card text or rule data. Card art displayed in the library and duels comes from
the separately installed publisher resources. The illustrations do not grant
rights in third-party characters or trademarks.

`content/deck-academy/` contains curated deck templates, fixed opening-hand
puzzles, lesson steps and native replay recordings. The content manifest binds
their file hashes and card identities. Retained replay provenance records are
used to verify initial decks, final board state, tutorial frame positions and
the matching publisher inputs. Recording and acceptance tools remain local
and are excluded from Git.

`content/ai/tokenizers/` retains the OpenAI tiktoken rank table and MIT notice.
It is embedded during compilation for local prompt-budget estimation.

See [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md) and
[COPYRIGHT.md](../COPYRIGHT.md) for attribution and rights scope.

## 中文说明

客户端装饰插画、品牌与硬币素材通过 `gpt-image-2` 辅助生成，随后完成裁切、
缩放与透明处理。文字、按钮、焦点与交互由原生控件绘制。装饰卡片不提供真实
卡文或规则；卡库和对局中的卡图来自独立安装的发行资源。

卡组学院保留构筑、固定起手残局、步骤和真实原生录像。清单及来源记录用于
验证内容身份、初始卡组、结束场地及教程帧位置。录制与验收工具仅供本地使用，
不提交 Git。第三方版权与许可范围见上方链接。
