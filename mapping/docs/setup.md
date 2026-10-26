# Mapping and game setup

[简体中文](setup.zh-CN.md)

The chain is **physical controller → Steam keyboard output → LoL WASD action**. Q/W/E/R in the control description mean the champion's skill slots, not necessarily keyboard letters.

## Core left-hand controls

- Left stick up/down/left/right → W/S/A/D → movement.
- L2 left trigger → E → Q skill.
- L1 left bumper → Left Shift → W skill.
- L3 left-stick click → R → R skill.
- Real mouse right button → E skill, handled by the game.

The final actions were confirmed by the user. The VDF does not include LoL's WASD bindings or the real mouse's driver/side-button settings.

## LoL WASD settings

Enable WASD mode, keeping W/A/S/D for movement. Set Q skill to E, W to Left Shift, E to Mouse right and R to R. Configure summoner spells/items on mouse side buttons as appropriate.

The original machine's WASD data explicitly records Q→E and E→Mouse right. The user confirmed W→Left Shift and R→R. The game may save only differences from defaults; traditional `evtCastSpell*` entries alone do not describe WASD behavior.

[wasd-keybinds.reference.json](../game-settings/wasd-keybinds.reference.json) is this project's reference format, not an import file. Apply changes through game settings; do not replace someone's full `PersistedSettings.json`.

## Other retained active bindings

These are preserved for a faithful backup but are not required for the left-hand play style:

- D-pad up/right/down/left → 1/2/3/4.
- Share → Tab; Options → Esc.
- Cross/Circle/Square/Triangle → Space/E/R/F.
- R1 → mouse wheel up.
- R2 → E on the trigger `click` input; L2 uses `edge`.
- R3 → R; the right stick retains `output_joystick = 2`.
- Center touchpad uses mouse mode; click → Mouse left.

Inactive old touchpad groups remain in the file. Labels such as `Jump`, `Use`, `Reload` are inherited display text, not LoL actions.

## Sustained R

D-pad down currently emits **3**. Assign that champion's R to 3 in the game if using it for a channelled/sustained ultimate. The VDF does not automatically switch this binding. See [champion notes](usage-notes.md).
