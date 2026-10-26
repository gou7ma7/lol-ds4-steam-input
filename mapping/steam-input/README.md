# Native Steam Input layout

[简体中文](README.zh-CN.md)

`lol-left-hand-ds4.vdf` is **UTF-8 Valve KeyValues text**, rooted at `controller_mappings`. It is not JSON or a developer's `game_actions_<appid>.vdf` action manifest.

Title: 英雄联盟 LOL WASD和鼠标. Original description: hold the controller in the left hand, use triggers + L3 for skills; hold the mouse in the right hand, use side buttons for items.

Obtained from the user's locally exported Steam layout on 2026-10-27, revision 9, type `controller_ps4`. The latest load record refers to desktop config 413080, community layout 3827898683.

- Steam link: `steam://controllerconfig/413080/3827898683`.
- Community page: https://steamcommunity.com/sharedfiles/filedetails/?id=3827898683 .

These come from the local export record. Visibility has not been changed or tested from another account.

Steam sometimes names the download cache `_legacy.bin`; this file's actual contents are still UTF-8 KeyValues. The repository uses `.vdf` for recognition and readable diffs.

## Sanitization

Removed creator SteamID, the local account-path URL, and old template localization. The user's title/description and all group/preset/settings data remain intact, including bindings, activators, active-group links and parameters.

Old labels such as `Jump`/`Reload` remain; see [game setup](../docs/setup.md) for real actions. Parsing round-trip and functional-data equality were verified; a second Steam installation import was not.

Do not upload full Steam userdata, global settings, device preferences or controller logs; they are not required to share a layout.

## Exporting again

Use Steam's layout export UI and retain both the native VDF and share link. Paths vary by client/export mode: official documentation mentions `userdata/<account>/241100/remote/controller_config/...`; recent clients also use `steamapps/common/Steam Controller Configs/...` and `steamapps/workshop/content/241100/...`.

Use actual export results, not a universal cache-path assumption. Double-clicking a GitHub-downloaded VDF does not import it. Prefer the native link, then manual reconstruction if unavailable.

Format reference: [Valve KeyValues / IGA documentation](https://partner.steamgames.com/doc/features/steam_controller/iga_file), which explains the shared format and distinguishes player layouts from developer manifests.

The user originally modified Steam's generic keyboard/mouse template. MIT licensing does not transfer Valve's software/template/trademark rights; extensive template localization was removed.
