# Third-party components

Yu-Gi-Oh! Fight derives its preserved game runtime and C# sources from the local
`../masterduel` project. The notices below retain that project's upstream source
and license provenance. Historical archived components are references only and
are not required by this new client's launcher.

## YgoMaster

Source: https://github.com/pixeltris/YgoMaster

Pinned release: v1.79; commit: c24aa153f78c72006e904174043b1c0c553680da.

The historical upstream source is archived in `../masterduel-legacy-20261101/vendor/YgoMaster-src`, unmodified. Its MIT license is preserved as `vendor/YgoMaster-LICENSE.txt` and in the source checkout. These are the initial Windows/Wine investigation components. The original official Master Duel executable is not included.

## Independent native Mac client and resources

The current game integration uses the public MDPro3 macOS release `1.4.7-2`, distributed by MyCard. Sources: https://code.moenext.com/sherry_chaos/MDPro3 and https://code.moenext.com/mycard/MDPro3 ; publisher metadata is retained under `vendor/MDPro3-mac-metadata/`.

The source-built variant compiles the full Assembly-CSharp from the matching release commit `72fb92ad02d173aeb5a5bb06f23603d892aec9c1`. Original Scripts/Tools archives and per-file identities are retained in `vendor/MDPro3-release-source/`; editable derived sources are in `game/`. Unity-generated script and job registration code is reconstructed directly from publisher metadata/IL. Compile-only references use Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3, NETStandard.Library.Ref 2.1.0, and UnityCsReference declarations; these reference supplements are not shipped as runtime libraries. Individual notices in third-party source trees are preserved.

The native client contains Unity runtime components, YGOPro libraries, and Master Duel-derived artwork and animation assets. These retain their respective copyrights and file notices. The current upstream source/resource repositories do not advertise a root license; a license from an older GitHub mirror is not applied to current resources. Native libraries are kept unchanged to preserve their matching ABI. This local project does not establish new redistribution rights or affiliation with Konami.

## Community native rule engine

The separate native engine is directly compiled from https://github.com/edo9300/ygopro-core at commit `6864373c24d7313c8e451df17877c12c4f713c46`. Its AGPL-3.0-or-later license and corresponding source are archived in `../masterduel-legacy-20261101/vendor/ocgcore/`. Lua and its copyright are retained in the pinned submodule. The local engine integration/smoke program also uses AGPL-3.0-or-later, as recorded in the archived `native/README.md`.

Card script and database source snapshots come from Project Ignis at commits recorded in the archived `native/provenance.json`. Their individual source notices are retained. Static source presence does not establish complete rule equivalence with Konami's engine.

## Windows Mono runtime

The historical payload, archived outside the current project, comes from YgoMaster's official `YgoMaster-Linux-Data-v1.zip` release asset, which the upstream [Linux documentation](https://github.com/pixeltris/YgoMaster/blob/v1.79/Docs/Linux.md) identifies as a reduced Windows Mono distribution. Mono runtime/class libraries generally use MIT, with some third-party BSD components; preserved notices are in `vendor/Mono-LICENSE.txt`. The license file is from https://github.com/mono/mono/blob/main/LICENSE and identifies per-component terms. Source and associated notices are available at https://github.com/mono/mono.

## REA development tooling

Source: https://github.com/morluto/rea ; npm package: `rea-agents@6.8.0`.

REA was used by the original project for development investigation and is not a launcher runtime dependency. Its own package retains its MIT license and third-party notices. Static reports preserve REA tool/provider provenance and their limited coverage.

## Original client and compatibility runtimes

The retained legacy launcher research would require a separately obtained official Master Duel client and compatible Wine/CrossOver installation. These are not bundled or required by Yu-Gi-Oh! Fight. The project is unofficial and is not affiliated with Konami.

## OpenAI tiktoken tokenizer data

Source: https://github.com/openai/tiktoken ; reference version 0.12.0.
The `o200k_base` merge-rank table is embedded in the source-built assembly for
local prompt budgeting. Its pre-tokenization expression is used by the local
BPE counter. The rank table and original notice are retained under
`content/ai/tokenizers/`.

MIT License

Copyright (c) 2022 OpenAI, Shantanu Jain

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
