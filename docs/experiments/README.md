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
