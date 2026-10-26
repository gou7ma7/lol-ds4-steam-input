# Left-hand DS4 + right-hand mouse mapping

[简体中文](README.zh-CN.md) · English

This module is independently usable. It contains a native Steam Input layout and the LoL WASD settings needed to turn controller outputs into skill actions. No feedback application is required.

## Controls

- Left stick: WASD movement.
- L2: Q skill; Steam emits keyboard E.
- L1: W skill; Steam emits Left Shift.
- Mouse right: E skill, assigned directly in LoL.
- L3: R skill; Steam emits R.

Only the controller's left half is used. The right hand uses a real mouse for aiming, interaction and additional bindings. See [full setup](docs/setup.md).

## Apply manually

1. Connect a DS4 and enable its Steam Input support.
2. Open `steam://controllerconfig/413080/3827898683` in Steam, preview the layout and apply it to the desktop configuration. Name: 英雄联盟 LOL WASD和鼠标.
3. Enable LoL's WASD mode and assign Q→E, W→Left Shift, E→Mouse right, R→R in that mode's settings.
4. Test movement and skill use in practice mode before normal play.

413080 identifies Steam's desktop layout, not LoL. Save your previous desktop layout first if you want to return to it. The Steam link was confirmed in the original user's local export/load record; access from other accounts is unverified. If unavailable, rebuild the core controls using the setup guide. Downloading or double-clicking a VDF does not automatically import it.

## Champion and interaction notes

For sustained/channelled R, D-pad down can avoid accidentally moving the left stick while pressing L3. The exported layout emits **3** there, so set that champion's R to 3 in the game. Passive-E champions such as Teemo/Twisted Fate, or champions using Q/W very frequently, can use a per-champion right-button swap. Some interactions, including Frost Gates and destination selection, need **Ctrl + left click** in the user's setup. These are manual notes, not automatic code; see [usage notes](docs/usage-notes.md).

## Files and AI use

- [Native VDF](steam-input/lol-left-hand-ds4.vdf), [format/provenance](steam-input/README.md), [metadata](steam-input/metadata.json).
- [Game reference JSON](game-settings/wasd-keybinds.reference.json): reference only, not a Riot import file.
- [AGENTS.md](AGENTS.md) and [English prompt](prompts/agent.en.md) / [Chinese prompt](prompts/agent.zh-CN.md).
- [MIT license](LICENSE).
- [Related mapping work](docs/related-work.md).

The layout retains inactive groups and old unused bindings for faithful backup. Functional group/preset/settings data matches the native export. Second-computer import remains untested.
