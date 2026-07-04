# Agent Handoff

## Source Of Truth

Read `docs/2026ciga策划案-完整表格.md` first. It was copied from `D:/Downloads/2026ciga策划案 (5).md` with UTF-8 intact. `docs/design-doc.md` is only an index now.

## Main Runtime Files

- `Assets/Subspace/Scripts/Runtime/SubspaceScoreResolver.cs`: main scan scoring, table effects, and reward upgrade bonuses.
- `Assets/Subspace/Scripts/Runtime/SubspaceTileData.cs`: persistent tile-effect flags and tile tooltip output.
- `Assets/Subspace/Scripts/Runtime/SubspaceGameDirector.cs`: reward flow, reroll flow, monster pressure, upgrade application.
- `Assets/Subspace/Scripts/Runtime/SubspaceBoardController.cs`: symbol refresh and view sync from tile state.
- `Assets/Subspace/Scripts/Runtime/SubspaceUIController.cs`: enemy ability panel, last damage tooltip, HP fill.
- `Assets/Subspace/Scripts/Definitions/SubspaceUpgradeDefinition.cs`: upgrade enum.

## Asset Pool

The reward pool is in:

- `Assets/Subspace/Generated/Resources/Subspace/SubspaceGameConfig.asset`

New upgrade assets are under:

- `Assets/Subspace/Generated/Upgrades/`

Legacy `Number_*.asset` files are still in the repo but should stay out of `startingSymbols` and rewards unless the design changes.

## Verification

`dotnet restore ciga-cillyroom.sln` then `dotnet build ciga-cillyroom.sln --no-restore` succeeds. Current warnings are unrelated existing Unity/Spine analyzer warnings.

## Follow-Up Risks

- Some table cells in the design source are truncated. Current interpretations are intentionally simple and should be revisited when design clarifies exact formulas.
- Monster abilities still map to the existing pressure enum. If the team wants exact named monsters from the table, add explicit monster ability definitions rather than overloading the enum further.
- If UI prefab references still point at old tooltip components, `SubspaceUIController.EnsureDamageTooltipTarget()` rebinds at runtime, but scene cleanup can make it tidier.
