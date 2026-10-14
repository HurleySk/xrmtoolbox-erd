# 004 Denormalised lookups as row labels

Date: 14 Oct 2026

## Change
A lookup is a *shortcut* when the table reaches the same target through another of its lookups, and the last hop of that path is a lookup column with the same name (Component.projectid when Component.structureid → Structure.projectid exists). Shortcuts are drawn as "→ Project (via Structure)" on the row instead of a line. `--shortcuts` (`DrawShortcutLookups`) draws them.

Rejected first: hiding any lookup whose target is reachable by any path. That hit 62-71% of lookups on the real solutions, including lookups with a different meaning (Task.assignedto → User when Task → Project → Manager → User). Requiring the path to use required lookups found nothing on real-b, which has only 20 required lookups.

## Run 004a (bug)

| Fixture | Edges | Crossings | Bends | Score |
|---------|-------|-----------|-------|-------|
| assets | 8 | 0 | 16 | 26.7 |
| project-delivery | 67 | 13 | 154 | 309.2 |
| real-a | 48 | 7 | 100 | 219.8 |
| real-b | 89 | 59 | 212 | 613.8 |

The numbers looked great and were wrong. Each lookup was checked on its own, so two lookups could hide each other: a structure table's lookups to two ancestors were each hidden "via" the other. Every component table lost its line to the structure table, and the structure table lost all its lines. The paths also went through a reference lookup (a parent's "latest child record" lookup, which points back down the hierarchy), which isn't a parent chain.

## Run 004b (fix)
- Per table, keep the lookup that explains the most of its siblings, hide what it explains, and repeat. A hidden lookup always names a lookup that stays drawn.
- A path never steps between two tables that look each other up.
- A required lookup is never hidden behind an optional one.

| Fixture | Edges | Crossings | Bends | Score | vs 003 |
|---------|-------|-----------|-------|-------|--------|
| assets | 8 | 0 | 16 | 26.7 | crossings 1 → 0 |
| project-delivery | 67 | 13 | 154 | 309.1 | crossings -50% |
| real-a | 103 | 119 | 272 | 1031.3 | crossings -75% |
| real-b | 101 | 82 | 250 | 767.9 | crossings -47% |

## Notes
- real-a's densest page now reads as a hierarchy five levels deep down to ten component tables, each component row saying "→ Project (via Structure)" or similar. The lines left between components are real component-to-component lookups.
- Since 001, crossings are down 88% on real-a and 92% on real-b.
- real-b's worst page now has 8 tables and 36 lines. A core table there has six self-lookups, and other tables have several lookups to the same parent.

## Verdict
004b kept, default on. Tests in `ShortcutTests` pin the rules, including the 004a bug.
