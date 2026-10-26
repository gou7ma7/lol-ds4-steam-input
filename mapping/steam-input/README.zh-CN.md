[English](README.md)

# Steam Input 原生布局

## 文件与来源

`lol-left-hand-ds4.vdf` 是 **UTF-8 的 Valve KeyValues 文本**，根节点为 `controller_mappings`。不是 JSON，也不是游戏开发者使用的 `game_actions_<appid>.vdf` 动作清单。

布局标题：英雄联盟 LOL WASD和鼠标。原描述：左手拿手柄，扳机键 + L3 放技能；右手拿鼠标，侧键按装备。

2026-10-27 从本机 Steam 已导出的社区布局取得，修订号 9，控制器类型 `controller_ps4`。Steam 最新加载记录对应桌面配置 413080、社区布局 3827898683。

- Steam 链接：`steam://controllerconfig/413080/3827898683`。
- 社区页面：https://steamcommunity.com/sharedfiles/filedetails/?id=3827898683 。

链接来自本机导出记录，没有再次发布或变更其可见性，也没有验证其他账号能否访问。

Steam 的下载缓存有时以 `_legacy.bin` 命名；本次文件的实际内容仍是 UTF-8 KeyValues 文本。因此仓库使用便于识别、比较和编辑的 `.vdf` 扩展名。

## 公开版本的清理

移除了作者 SteamID、包含账号目录的本机 URL，以及早期通用模板的大段多语言说明。保留用户的标题和描述，所有 group / preset / settings 内容保持一致，未改实际键位、激活器、有效动作组和参数。

模板遗留的 `Jump`、`Reload` 等绑定显示标签仍在文件中，实际技能动作以 [游戏设置](../docs/setup.zh-CN.md) 为准。清理文件通过解析往返和功能数据一致性检查；另一台 Steam 的导入尚未验证。

不要上传整个 Steam `userdata`、全局配置、设备偏好文件或控制器日志。它们不是单个布局的必需内容。

## 再次导出

在 Steam 控制器布局界面使用导出功能，保留一份原生 VDF 与分享链接。Steam 的保存位置因版本和导出方式而异：官方文档提到 `userdata/<账号>/241100/remote/controller_config/...`；当前客户端也使用 `steamapps/common/Steam Controller Configs/...` 与 `steamapps/workshop/content/241100/...`。

应以具体导出结果为准，不要把一个缓存目录误认为所有版本通用的安装位置。通过 GitHub 下载 VDF 后不能只双击完成导入；优先使用原生分享链接，失效时按文档手动重建。

格式参考：[Valve 官方 KeyValues / IGA 说明](https://partner.steamgames.com/doc/features/steam_controller/iga_file)。此页面解释相同的数据格式，并区分玩家保存布局和开发者动作清单。

此布局最初基于 Steam 通用键鼠模板进行用户修改。Valve 的软件、模板来源和商标权利不因本项目的 MIT 许可而转让；仓库已去掉模板的大段本地化说明。
