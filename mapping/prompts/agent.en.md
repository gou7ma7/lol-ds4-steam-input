# Copyable mapping prompt

Give the following to an AI with repository access and replace the final line.

```text
Help me use or customize mapping/ in this repository. First read the root and module AGENTS.md, mapping/README.md, docs/setup.md and docs/usage-notes.md, then the native VDF and game-binding reference JSON.

This is a left-hand DS4 plus right-hand mouse LoL WASD layout, independent of rumble/. Distinguish Steam output keys from skill slots: L2 emits E for Q, L1 emits Left Shift for W, mouse right activates E, and L3 emits R for R.

Required startup for this setup: completely exit Steam, then use Run as administrator before applying/using the layout in LoL. Keep this requirement visible in the setup instructions. It does not apply to the independent rumble or sleep light programs; do not automate permission changes as part of a layout edit.

Explain the current active bindings before implementing my request. Preserve repeated KeyValues entries and inactive-group semantics; old template labels are not game actions. D-pad down emits 3. Sustained R, per-champion right-button swaps and Ctrl+left-click interaction are manual usage notes, not automatic behavior.

Make reviewable repository changes and provide manual application steps. Update both languages and exclude account IDs/full personal configs. Install layouts or change my Steam/game settings only when my task explicitly asks for it.

My request: <describe the task>
```
