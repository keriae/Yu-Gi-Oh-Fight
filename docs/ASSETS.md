# Client artwork and content

Yu-Gi-Oh! Fight is an unofficial, non-commercial fan project for learning,
research and community exchange. Original project content and third-party
assets have distinct rights scopes, described in [COPYRIGHT.md](../COPYRIGHT.md).

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

## Promotional typography

The trailer uses modified, self-hosted Cinzel Decorative, Cinzel and Noto
Serif SC fonts from the Google Fonts repository. These font assets remain
under the SIL Open Font License 1.1. Original notices, the pinned upstream
revision, source and output hashes, renamed families, and subset details
are retained in [`media/promo/public/fonts/`](../media/promo/public/fonts/).
The display title has a gold engraved treatment; English and Chinese
captions are measured and placed on one line. The fonts are media-authoring
assets, separate from the game's publisher fonts.

## 中文说明

本项目为面向学习、研究与社区交流的非官方、非商业爱好者项目。原创内容与
第三方资源的权利范围各自独立，详见 [COPYRIGHT.md](../COPYRIGHT.md)。

客户端装饰插画、品牌与硬币素材通过 `gpt-image-2` 辅助生成，随后完成裁切、
缩放与透明处理。文字、按钮、焦点与交互由原生控件绘制。装饰卡片不提供真实
卡文或规则；卡库和对局中的卡图来自独立安装的发行资源。

卡组学院保留构筑、固定起手残局、步骤和真实原生录像。清单及来源记录用于
验证内容身份、初始卡组、结束场地及教程帧位置。录制与验收工具仅供本地使用，
不提交 Git。第三方版权与许可范围见上方链接。

宣传片标题采用古金雕刻式字体，英文与中文字幕同一行显示。自托管字体基于
Cinzel Decorative、Cinzel 和 Noto Serif SC，保留 SIL OFL 1.1、固定来源
版本、修改版名称及子集说明，详见上方字体素材目录。
