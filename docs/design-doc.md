# Anchor Design Doc Index

This file is now an index. The authoritative design source is:

- `docs/2026ciga策划案-完整表格.md`

Use the complete table version for element names, base scores, tile effects, item rewards, and monster ability wording. The previous `design-doc.md` copy was missing the right side of several tables and is no longer safe as an implementation source.

Current implementation notes:

- The board starts as `6x6`.
- The initial scanner is `2x2`.
- Play uses the new element system from sections 5.1 and 5.2.
- End-of-level rewards use growth upgrades/items instead of granting old symbols.
- Legacy number symbols may still exist as orphaned asset files, but they are not in `SubspaceGameConfig.startingSymbols`.
- Persistent tile effects live on `SubspaceTileData` and should survive symbol refreshes.
- The right side combat UI shows enemy ability in the former score panel and last damage in the former round panel.

When updating mechanics, keep the table document and this index in sync.
