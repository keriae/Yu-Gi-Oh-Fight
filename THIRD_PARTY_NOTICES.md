# Third-party notices

Yu-Gi-Oh! Fight is an unofficial, non-commercial fan project maintained by
[keriae](https://github.com/keriae), for personal learning, research and
community exchange. It is not affiliated with or endorsed by Konami.
Yu-Gi-Oh! names, cards, characters, artwork, animations, music, sound effects
and other game assets belong to their respective rights holders.

These notices describe component provenance and reproduce applicable notices;
they do not grant additional rights in third-party software or game assets.
Original notices and permissions in retained source files and separately
obtained runtimes continue to apply. The project's non-commercial purpose
does not restrict rights granted by individual component licenses, including
MIT and the SIL Open Font License. The project does not assign a repository-wide
license to all code and assets; see [COPYRIGHT.md](COPYRIGHT.md) for the scope
and non-commercial purpose of original project contributions.

## MDPro3 and the game runtime

- Sources: [MDPro3](https://code.moenext.com/sherry_chaos/MDPro3) and
  [MyCard distribution](https://code.moenext.com/mycard/MDPro3).
- Release: `1.4.7-2`; source commit:
  `72fb92ad02d173aeb5a5bb06f23603d892aec9c1`.
- References: the [matching distribution README](https://code.moenext.com/mycard/MDPro3/-/blob/72fb92ad02d173aeb5a5bb06f23603d892aec9c1/README.md)
  and the [official MDPro3 README](https://code.moenext.com/sherry_chaos/MDPro3/-/blob/e177cd8d53092ae9249e37b38d3cef0040e41095/README.md)
  document Unity development requirements and external resource sources.
- Matching source archives and their file hashes are retained in
  `vendor/MDPro3-release-source/`. Editable derived code is under `game/`.
- Publisher resource checksums are under `vendor/MDPro3-mac-metadata/`.

The matching upstream repositories do not provide a root license for all
current sources and resources, and the READMEs cited above do not state a
non-commercial restriction. Yu-Gi-Oh! Fight's non-commercial purpose is its
own project policy. A license from an older mirror is not applied to the
matching sources. The build obtains and verifies the publisher runtime separately;
Unity scenes, AssetBundles, card data and matching native libraries remain
publisher components. This repository does not include the official Master
Duel executable.

The runtime includes Unity and Unity packages such as TextMesh Pro,
Addressables and the Input System, as well as the community YGOPro rule
libraries, card scripts, databases and WindBot. These retain their original
terms. The native rule libraries are used unchanged with the matching MDPro3
ABI. Relevant upstream projects include
[ygopro-core](https://github.com/edo9300/ygopro-core),
[Project Ignis scripts](https://github.com/ProjectIgnis/CardScripts),
[Project Ignis databases](https://github.com/ProjectIgnis/BabelCDB), and
[WindBot](https://github.com/IceYGO/windbot).
The license of a separate upstream project is not a substitute for the terms
of the exact publisher files.

## Libraries retained in source and runtime

The `game/Assets/Tools/` and upstream source trees include AssetStudio,
StandaloneFileBrowser, 7-Zip/LZMA support, and other compatibility code.
Their source-level notices and restrictions remain intact. Unity runtime
libraries, Mono, Newtonsoft.Json, UniTask, DOTween and other publisher-managed
dependencies retain their component licenses. The source build checks their
publisher hashes and does not replace their licensing information.

The retained Mono license collection is `vendor/Mono-LICENSE.txt`. The
Mersenne Twister implementation in
`game/Assets/Scripts/Tools/mt19937ar.cs` retains the original copyright and
conditions of Makoto Matsumoto, Takuji Nishimura, Mutsuo Saito and Mitil.
For binary distributions, its notice is reproduced below.

### Mersenne Twister notice

Copyright (C) 1997–2002, Makoto Matsumoto and Takuji Nishimura.
Copyright (C) 2005, Mutsuo Saito. Copyright (C) 2006, Mitil.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
   this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.
3. The names of its contributors may not be used to endorse or promote
   products derived from this software without specific prior written
   permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.

## MIT components

The following retained components carry the MIT permission text reproduced
below. Their respective copyright notices must remain with substantial copies.

### AssetStudio

Source: [Perfare/AssetStudio](https://github.com/Perfare/AssetStudio).
Original [MIT notice](https://github.com/Perfare/AssetStudio/blob/master/LICENSE):

Copyright (c) 2016 Radu.
Copyright (c) 2016-2020 Perfare.

The notice covers the upstream library portions retained under
`game/Assets/Tools/AssetStudio/`. MDPro3-specific changes retain the provenance
and rights scope described above.

### StandaloneFileBrowser

Source: [gkngkc/UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser).
Original [MIT notice](https://github.com/gkngkc/UnityStandaloneFileBrowser/blob/master/LICENSE.txt):

Copyright (c) 2017 Gökhan Gökçe.

The upstream library portions retained under
`game/Assets/Tools/StandaloneFileBrowser/` use the MIT permission text below.
The macOS native selector under `native/StandaloneFileBrowser/` derives from
upstream commit `04a5d49ed2545556da8a7192e86c69bd47641f10`; its original file
hashes and local changes are recorded in `provenance.json`. Modifications
Copyright (c) 2026 keriae, under the same MIT terms. The original license is
also retained beside its source and in the application documentation.

### OpenAI tiktoken

Source: [openai/tiktoken](https://github.com/openai/tiktoken), reference version
`0.12.0`. Copyright (c) 2022 OpenAI, Shantanu Jain.

The `o200k_base` merge-rank data and original notice are retained in
`content/ai/tokenizers/`. The source-built assembly embeds the rank table for
local prompt budgeting. Its original MIT notice is also retained as
`content/ai/tokenizers/LICENSE.tiktoken`.

### Brotli decoder

Source: [google/brotli](https://github.com/google/brotli).
Copyright 2015, 2017 Google Inc. All Rights Reserved.

The C# decoder retained under `game/Assets/Tools/AssetStudio/Brotli/` declares
the MIT license in its source headers.

### MIT permission text

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Build-only tools

The build uses a pinned .NET SDK, Mono.Cecil, Microsoft reference assemblies
and UnityCsReference declarations. Their package licenses remain applicable.
Reference supplements are used for compilation and compatibility checks and
are not shipped as replacement runtime libraries. Package identities and
download sources are recorded in `tools/source-toolchain.json` and
`vendor/MDPro3-release-source/manifest.json`.

The promotional media sources under `media/promo/` use
[React](https://github.com/facebook/react) and
[Remotion](https://www.remotion.dev/license). These are build-only media
dependencies, separate from the game runtime. Their package licenses and
Remotion's applicable usage terms remain in effect; dependencies are not
vendored in this repository.

## Promotional fonts

The promotional media sources retain modified fonts derived from
[Cinzel](https://github.com/google/fonts/tree/main/ofl/cinzel),
[Cinzel Decorative](https://github.com/google/fonts/tree/main/ofl/cinzeldecorative)
and [Noto Serif SC](https://github.com/google/fonts/tree/main/ofl/notoserifsc).
They are distributed under the SIL Open Font License 1.1, with their original
notices in `media/promo/public/fonts/OFL.Cinzel.txt`,
`OFL.CinzelDecorative.txt` and `OFL.NotoSerifSC.txt`. The modified family names
are Fight Promo Serif Latin, Fight Promo Display and Fight Promo Serif SC.
Source revisions, hashes and modifications are recorded beside them in
`provenance.json`. The rendered video does not embed these fonts as game
runtime components; the OFL's font redistribution conditions continue to
apply to the authoring assets.

## 中文说明

Yu-Gi-Oh! Fight 由 keriae 维护，是面向个人学习、研究与社区交流的非官方、
非商业爱好者项目，与科乐美（Konami）无关联，也未获得其背书。游戏王名称、
卡牌、角色、图像、动画、音乐、音效及其他游戏资源归各自权利人所有。

第三方代码、运行时与资源保留各自的许可和版权。本文件用于说明来源，
不增加第三方代码或资源的授权，也不为整个代码与资源树授予统一许可证。
项目的非商业定位不改变各组件已有许可授予的权利，包括 MIT 与 SIL OFL。
原创贡献的非商业定位及适用范围见 [COPYRIGHT.md](COPYRIGHT.md)。

所核对的 MDPro3 固定版本没有覆盖全部源码与资源的根许可证，上方链接的
README 说明了开发与资源来源，但没有非商用条款。本项目的非商业定位属于
Yu-Gi-Oh! Fight，不能套用旧镜像或其他上游项目的许可证。发布安装包或传播
视频时，应遵守所使用内容的相关条款。
