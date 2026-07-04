# AGENTS.md

本文件给 Codex（以及任何 AI 编程代理）在这个 Unity 项目里工作时提供约束。核心目的：让代理与用户高效协作，不迷失在自动化测试循环里，改代码前先确认对象和方向。

## 项目概览

`Golden-Dolphin` / `cillyRoom` 是一个 Unity 肉鸽计分游戏原型。玩家在 6x6 网格上用锚定框选区域，点击攻击后结算框内符号得分，累计到目标分数通关。当前在 MVP 阶段，正在叠加"地块培养系统""Tooltip 信息悬浮窗""地块培养视觉表现"等功能。

## 技术栈

- Unity `2022.3.62f3c1`
- Spine（`Assets/Spine`，怪物/角色骨骼动画）
- Unity MCP：`com.coplaydev.unity-mcp`，HTTP 端点 `http://127.0.0.1:8080/mcp`，用于在编辑器内执行代码、读控制台、操作资源。详见 `docs/unity-mcp.md`
- 脚本命名空间统一为 `Subspace`
- 核心代码在 `Assets/Subspace/Scripts/`，分为 `Runtime`、`Definitions`、`Editor` 三层

## 架构要点（改代码前必读）

项目里有**两套**游戏控制器，改之前必须确认场景里实际跑的是哪一套：

1. **旧单体控制器** `SubspaceGameController`：一个大 MonoBehaviour 自己建 UI、管网格、结算、刷新。当前 `SampleScene` 实际运行的就是它。格子 UI 用 `Image`/`Text` 列表，不挂 `SubspaceSymbolCellView`。
2. **组件化版** `SubspaceGameDirector` + `SubspaceBoardController` + `SubspaceUIController` + `SubspaceSelectionController` + `SubspaceSymbolCellView` 等：各司其职，由编辑器生成器 `SubspaceGeneratorWindow` 搭建。当前场景没有使用它。

**陷阱**：改了组件版但场景跑的是旧版，会导致改动完全不生效。改之前先用 Unity MCP 在运行时确认 `FindObjectOfType` 的结果，或者直接问用户"你要改哪一套"。

数据层（地块培养系统）已抽象为 `SubspaceTileData` / `SubspaceSymbolData` / `SubspaceTileBuffInstance`，规则集中在 `SubspaceTileRulebook`。结算入口是 `SubspaceScoreResolver.Calculate`，有符号数组和地块数组两个重载。

## 工作风格（最重要）

这条比任何技术细节都重要。违反它会浪费大量时间和用户的信任。

- **发现根因或分叉时，先停下来汇报，再动手。** 例如发现"场景用的是旧控制器，不是你改的那套"，正确的做法是告诉用户并问怎么走，而不是自己决定把旧控制器也接一遍。
- **验证有节奏：最多 2 次探测，超了就问人。** 编译检查加 1 次运行时探测是合理的；连着跑十几个反射调用、反复查同一个字段，就是失控了。
- **不陷入命令循环。** 如果同一个问题需要第三次跑命令验证，停下来跟用户说清楚现状和你的困惑，让人来定。
- **改代码前先说要改什么、改哪个文件、为什么。** 用 commentary 频道说一两句，再动手。
- **每完成一个有意义的小步骤，简短反馈进展。** 不要闷头做完一长串才开口。
- **诚实承认卡点。** 卡住了就说卡住了，不要用更多命令掩盖。

## Unity MCP 验证流程

通过 `http://127.0.0.1:8080/mcp` 的 `execute_code` 工具在 Unity 内执行代码。注意事项：

- `execute_code` 用 CodeDOM 动态编译，**不带 `using` 命名空间**。必须写全名，例如 `UnityEngine.Object.FindObjectOfType<...>()`，不能用 `Object.FindObjectOfType`，否则 `Object` 会被解析成 `System.Object`。
- 片段必须有 `return` 值，否则报"not all code paths return a value"。
- 新增 `.cs` 文件后，Unity 不会自动生成 `.meta`，导致脚本不被编译。需要手动补 `.meta` 或通过 `AssetDatabase.ImportAsset` 强制导入。
- 重编译会强制退出 Play 模式；验证运行时行为需要重新进 Play。

推荐的验证顺序（严格按此走，不超步）：

1. 改完代码，触发 `AssetDatabase.Refresh` + `CompilationPipeline.RequestScriptCompilation`。
2. 等编译完成，读控制台 `read_console`，只看 error。零 error 才继续。
3. 如需运行时验证，进 Play 后做**一次** `execute_code` 探测关键状态。探测结果异常就汇报用户，不要自己连查三四次。

## 代码约定

- 命名空间 `Subspace`，类名前缀 `Subspace`。
- 数据用 `ScriptableObject` 定义（`SubspaceSymbolDefinition`、`SubspaceGameConfig`、`SubspaceLevelDefinition`、`SubspaceArtSet`、`SubspaceTextConfig`）。
- 运行时 UI 多为代码动态创建，不依赖预制体。新增 UI 组件遵循同样风格。
- 中文字符串在 `.cs` 源码里用 `\uXXXX` 转义写入，避免编辑器或编码差异导致字符串损坏。显示文本集中在静态 `Text` 内部类或 `SubspaceTextConfig` 里。
- 改动尽量贴合现有模式，不引入新框架或外部插件。

## 不要做的事

- 不要重写整个项目或大规模重构，除非用户明确要求。
- 不要引入复杂外部插件。
- 不要改美术资源导入流程。
- 不要为了"完美架构"拖慢 MVP。
- 不要把所有逻辑塞进 UI 脚本。
- 不要回滚用户已有但与你任务无关的改动。
- 不要用 `git reset --hard` 等破坏性操作，除非用户明确要求。

## 已知遗留

- `SampleScene` 里有一个 `The referenced script (Unknown) on this Behaviour is missing!` 警告，是历史遗留，未处理。
- 仓库里有大量未提交的改动（Unity 自动导入、Spine 资产、之前任务的产物），这些不是本次任务的产物，不要回滚。
- 旧单体 `SubspaceGameController` 的 `AutoCreateController` 已被禁用，保留只为旧场景兼容。

## 设计理解（基于 docs/design-doc.md，持续迭代）

一句话概括：**能培养地块的类幸运房东游戏**。玩家用扫描框扫描亚空间区域，获得即时分数（现实稳定度），同时永久培养地块。地块效果在整个大局（跨关卡）保留，符号每回合刷新。

### 核心原则

1. **地块效果跨关保留**：SubspaceTileData.baseBonusScore、uffs、debuffs 在进入下一关时不重置。只有 currentSymbol 每回合随机刷新。Board 在关卡切换时只重新随机符号，不擦除地块状态。
2. **每个符号都有双重属性**：instantScore（即时得分）+ 	ileEffects（地块培养效果，可以是正面的也可以是负面的）。策划案里的"资源/锚定/威胁"分类是设计思路上的侧重，不是代码层面的互斥类型——不需要加 category 枚举。
3. **已形成地块不刷新符号锁死规则**：地块效果保留，但符号照常刷新。RerollOutside 应跳过"已形成"地块（有 baseBonus > 0 或 buffs/debuffs 非空）的符号刷新——不，符号照常刷新，只是地块效果保留。具体：RerollOutside 只刷新 currentSymbol，不动 aseBonus/buffs/debuffs（现有代码已经如此）。后续可能加"地块保护"升级让符号也不刷新，但当前不做。
4. **结算需要全局上下文**：SubspaceScoreResolver.Calculate 要能查询相邻格子状态、玩家升级/道具状态。选区从 RectInt 抽象为 List<Vector2Int>（异形扫描器：十字、L 形、整列等）。

### 待做的基建（按优先级）

- **选区形状抽象**：SubspaceSelectionShape（List<Vector2Int>），SubspaceScoreResolver.Calculate 接受 shape 而非 RectInt，SubspaceSelectionController 支持 shape。
- **结算上下文**：SubspaceScoreContext（地块数组 + 选区 shape + 玩家升级状态），传入 Calculate。
- **符号联动接口**：SubspaceSymbolDefinition 加 synergyId + synergyBonus，结算器加联动检测入口（先留空实现）。
- **跨关地块保留**：SubspaceBoardController.Build 在关卡切换时只重新随机符号，不 new 新 TileData，保留已有 baseBonus/buffs/debuffs。

### 不做的

- 不加 SubspaceSymbolCategory 枚举（分类是设计层概念，代码层每个符号都有 instantScore + tileEffects 即可）。
- 不做复杂状态机或行为树。
- 不改美术资源导入流程。

## 相关文档

- `docs/unity-mcp.md`：Unity MCP 端点配置与连接检查方法。
