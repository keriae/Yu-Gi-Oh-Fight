# Client UI

The client implements its controllers, controls, popups, input, deck editor,
duel HUD, rule-response prompts, card information, logs and field interaction
under `game/Assets/Scripts/YuGiOhFight/Client/`. The existing layout and
interaction behavior are preserved. The presentation module contains the
required element lookup, scrolling, tweens, effects, timelines, rendering
and scene interaction.

Card databases, card scripts, artwork, fonts, materials, animation, audio,
WindBot and the native rule libraries are reused. Their provenance is recorded
in [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md).

## Layout resource import

Scenes and prefab layouts are imported as view data. During a build,
`scripts/client_ui_resources.py` binds their MonoScripts and persistent type
references directly to project components using the explicit table in
`content/client-ui/serialized-bindings.json`. Text components with no custom
behavior bind directly to TextMesh Pro. The importer removes the identified
empty component slot, inert callback, and fingerprinted components whose script
types have no implementation in the publisher. It preserves native state-machine
indices and all valid rendering objects.

The importer writes independent cached outputs, verifies the rewritten objects,
and updates Addressables hashes, CRCs and bundle sizes. The build stages these
outputs before signing. `vendor/` remains the verified resource source.
The source manifest records import inputs and output hashes; packaging checks
that the staged resources match them.

There are no runtime identity bridges or old UI implementations. Code calls
project components directly and uses Unity and Addressables to instantiate
layouts. Preserve serialized field names and public callbacks used by layout
data, or update the corresponding resource import when changing them.

## Build validation

`SourceRegistrations` generates Unity source registration metadata from the
current source snapshot. `SourceValidation` resolves the compiled assembly's
types and external members, checks required native declarations, and rejects
references to retired UI identities. It does not require unused publisher types
to remain in the project.

`scripts/client_ui_runtime_metadata.py` imports the player's startup callback
table with the same explicit bindings. This preserves initializer timing,
including card materials and Unity Jobs. Shipping Mono and native smoke checks
exercise the compiled code and matching rule core.

```sh
python3 build.py
```

Changes to bindings or layout import require a complete build. For C# edits,
`python3 build.py --code-only` compiles and validates the source snapshot.
