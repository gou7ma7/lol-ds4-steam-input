# 相似振动反馈项目

[English](related-work.md)

资料核对日期：2026-10-27。查看了项目说明和部分源码，没有运行他人的程序。

- [buttplug-of-legends](https://github.com/kinkyacedev/buttplug-of-legends)：已归档的 Python 项目，使用同一 `127.0.0.1:2999/liveclientdata` 接口读取玩家名和事件。[源码](https://github.com/kinkyacedev/buttplug-of-legends/blob/master/league_thread.py) 把击杀、助攻转换成外设反馈，死亡时停转。面向 Intiface/Buttplug 兼容的成人外设，设备类型与 DS4 不同；2021 年已归档，不代表当前兼容。
- [Buttplug-Of-Legends-Unity](https://github.com/Furimanejo/Buttplug-Of-Legends-Unity)：后续 Windows/Unity 版本，作者描述按游戏事件累计分数控制外设强度，README 已标记弃用。本项目没有复制两者代码。

“本地比赛数据→物理反馈”的思路已有先例。本模块专门针对 USB DS4 第二代，包含净掉血、低血量/资源、击杀/助攻/连杀节奏、可配置预设，以及取消和过期反馈处理。

这次搜索没有找到经核实、与本项目这套单手 Steam 布局及 DS4 反馈实现完全一致的组合。这是有限搜索结果，不是“全球首个”的声明。
