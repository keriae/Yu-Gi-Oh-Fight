# Publisher Addressables catalog reader

Run `python3 scripts/dump-shipping-catalog.py` to inspect the fixed MDPro3
`1.4.7-2` catalog with the publisher Mono and Addressables reader. Inputs are
checked against publisher hashes before creating a separate diagnostic copy.

The tool prepares that copy through `CatalogReaderPrep`, compiles this reader
with the pinned toolchain in `tools/source-toolchain.json`, and runs it through
`native/managed-tool-runner.c`. It never installs diagnostic modifications in
the game. Reports and their provenance are written to ignored local artifacts.

The result projects catalog IDs, types, providers and dependency edges. It
omits runtime cache/hash options and does not replace the game's catalog or
prove coverage of dynamically loaded resources.
