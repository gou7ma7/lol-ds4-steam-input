# Mapping agent context / 按键映射 Agent 说明

- Scope: this entire `mapping/` subtree; it must work without `rumble/`.
- Read `README.md`/`README.zh-CN.md`, `docs/setup.md`, `docs/usage-notes.md`, then the VDF and reference JSON.
- Source of truth for physical outputs: `steam-input/lol-left-hand-ds4.vdf` and its active preset/group links.
- Source of truth for final actions: setup docs and `game-settings/wasd-keybinds.reference.json`.
- Core use: left-hand DS4 + right-hand mouse; L2 outputs E for Q, L1 outputs Left Shift for W, mouse right performs E, L3 outputs R for R.
- Do not confuse skill slot Q/W/E/R with the keyboard letter Steam emits.
- D-pad down currently emits 3. Sustained-R use needs a per-champion game R binding to 3; it is a note, not automatic switching.
- Passive E/high-frequency Q/W substitutions and Ctrl+left-click portal interaction are usage notes only.
- Parse Valve KeyValues with duplicate keys preserved; a plain JSON dictionary parser can lose repeated groups/bindings.
- Keep inactive groups distinguishable from active bindings. Template labels such as Reload are not LoL actions.
- Do not rewrite Steam/global game settings or expose private metadata as part of repo maintenance.
- Keep paired Chinese/English docs aligned and update metadata hash after an intentional VDF change.
- Prompts: `prompts/agent.zh-CN.md` and `prompts/agent.en.md`.
