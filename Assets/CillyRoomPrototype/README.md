# CillyRoom Prototype

This folder contains a playable Unity prototype for the requested Luck Be a Landlord style loop.

- Open `CillyRoom/原型生成器` and press `一键生成两个关卡` to create editable assets and a component-based scene hierarchy.
- The generated root is `CillyRoom Component Prototype`.
- Press Play in the current scene to test.
- Move the selection box with mouse drag, WASD, or arrow keys.
- Click `攻击` to score the selected numbers. Selected cells stay fixed; unselected cells reroll.
- Logic is split across scene components: `CillyRoomGameDirector`, `CillyRoomBoardController`, `CillyRoomSelectionController`, `CillyRoomActorController`, `CillyRoomUIController`, `CillyRoomBriefingController`, and `CillyRoomRewardController`.
- Replace art mainly on the `CillyRoom Art Rig` GameObject in the scene. It is grouped as backgrounds, player, enemy, effects, UI frames, and board symbols.
- Tune levels in `Assets/CillyRoomPrototype/Generated/Levels`.
- Tune symbols in `Assets/CillyRoomPrototype/Generated/Symbols`.
