# Agent entry point / Agent 入口

This repository contains two independently usable modules. 本仓库的两个模块可独立使用。

- `mapping/`: native Steam Input layout + LoL WASD configuration; no program or rumble dependency.
- `rumble/`: Windows USB DS4 feedback using the loopback Live Client API; no mapping/Steam dependency.
- Read the selected module's `AGENTS.md` and README before editing. 修改前先读取对应模块入口。
- Read `README.md` (English) or `README.zh-CN.md` (Chinese) for navigation and source-first build guidance.
- Keep both languages aligned and preserve module independence. 同步双语说明，保持模块独立。
- Do not put account IDs, local paths, device serials or personal match data in public files.
- Limit work to manual mapping and rumble feedback. Changes to other controller features are outside scope.
- Build and tests do not send controller commands; running the tray app/tests from its menu can.
- No publication or Steam/game installation is implied by a documentation/code edit.
- Copyable task prompts are in each module's `prompts/` directory; they are context, not automatic execution requests.
