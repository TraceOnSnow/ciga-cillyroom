# Teammate Update

## What Changed

The game now uses the element system from `docs/2026ciga策划案-完整表格.md` as the source of truth. The old visible symbols are no longer part of the play pool, and level-end rewards now come from the growth/item upgrade pool instead of symbol rewards.

Implemented element behavior includes:

- Resource scoring: signal node, torn space, overclock, data, prism, gravity flow, energy shard, multidimensional analysis, energy transition, resonance signal, chaos signal, void signal, blocking signal.
- Persistent tile effects: reality anchor, signal boost point, double excitation, growth node, reality link, space turbulence, energy element, signal sacrifice, data flow, cosmic prism, stable field, hot core, magnetic field, chaos field, signal conversion, signal enhancer, chaos stance.
- Reward items: signal stabilization, overload, limit scan, space preserve, time rewind, energy survey, cleaner robot, chaos conversion, damage control, last stand, double scan.

UI fixes:

- Right panel now shows enemy ability where the old last-damage box was.
- Last damage moved to the old per-turn box and keeps sorted hover details for damage sources.
- Enemy HP fill is forced to a filled horizontal bar so the red part shrinks with HP.
- Ship sprite tint is forced back to white when a real sprite exists, fixing the always-red ship.

## Notes

Some design text is truncated in the source table. For those cases the implementation follows the table intent with MVP-friendly behavior and records details in score tooltip lines.
