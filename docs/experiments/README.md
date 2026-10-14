# Layout experiments

Each run of a layout change gets a write-up here, losers included, so nothing is tried twice.

Fixtures:
- `assets`: synthetic, 6 solution tables, one page.
- `project-delivery`: synthetic, 41 solution tables with denormalised lookups, shared config tables and core tables.
- `real-a` (36 tables), `real-b` (41 tables): metadata snapshots of real solutions, kept outside the repo. Only totals are reported.

Metrics come from `solution-erd render`. Crossings and box hits matter most. The score is 10 x box hits + 3 x crossings + bends + length / 200, summed over pages.

| # | Change | Kept? |
|---|--------|-------|
| [001](001-baseline.md) | Baseline: Louvain pages, hub stubs, Sugiyama left to right, rectilinear routing | baseline |
| [002](002-own-lookups-only.md) | A page draws only its own tables' lookups | kept |
| [003](003-off-page-labels.md) | Off-page lookups as "→ Target" row text instead of stub boxes | kept |
| [004](004-shortcut-lookups.md) | Denormalised lookups as "→ Target (via X)" row text (004a bug, 004b fix) | 004b kept |

## What 002-004 changed, and what they didn't

All three changed **which relationships are drawn as lines**. The placement and routing code is the same as in 001. Most of the drop in crossings comes from drawing fewer lines, and crossings grow faster than the line count, so removing a line removes several crossings.

| Fixture | Lines 001 → 004b | Crossings 001 → 004b | Crossings per line |
|---------|------------------|----------------------|--------------------|
| project-delivery | 179 → 67 (-63%) | 100 → 13 | 0.56 → 0.19 |
| real-a | 271 → 103 (-62%) | 982 → 119 | 3.62 → 1.16 |
| real-b | 337 → 101 (-70%) | 1027 → 82 | 3.05 → 0.81 |

No relationship was dropped from the diagram. Each removed line is still shown as text on its lookup row: "→ Target" when the target is on another page or outside the solution, and "→ Target (via X)" for a shortcut. The cost is that the reader follows a name, not a line. The `--stubs` and `--shortcuts` options (in the plugin: "Draw off-page tables as boxes", "Draw lines for shortcut lookups") bring the lines back.

From 005 on, a change to placement or routing is measured with the set of drawn lines held fixed, so the numbers show the layout and not the line count. Changes to what is drawn report lines drawn next to crossings.
