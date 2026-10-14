# 001 Baseline

Date: 14 Oct 2026

## Setup
- Noise filter: system lookups hidden, out-of-solution lookup targets drawn as stubs, polymorphic lookups over 3 targets only drawn to solution tables.
- Pages: split when a solution has more than 15 tables. Hubs are account, contact, systemuser, team and businessunit, plus any table with at least 6 neighbours. Louvain finds communities among the rest. Each hub's home page is the page it links to most; every other page that touches it shows a stub.
- A page shows every relationship that touches one of its tables, in either direction.
- Layout: MSAGL Sugiyama, left to right, parents on the left. Rectilinear routing with a port on each lookup row.

## Results

| Fixture | Pages | Edges | Crossings | Bends | Box hits | Score |
|---------|-------|-------|-----------|-------|----------|-------|
| assets | 1 | 11 | 1 | 24 | 0 | 41.8 |
| project-delivery | 7 | 179 | 100 | 432 | 0 | 1136.7 |
| real-a | 7 | 342 | 982 | 769 | 0 | 5148.3 |

## What the pages look like
- `assets` reads fine.
- `project-delivery`, Project page: 10 full tables and 24 stubs. Project is a hub and its home page is this one, so every table elsewhere that looks up Project shows up here as a stub with a line. Same pattern on real-a, where two pages carry 113 and 106 lines and 926 crossings between them.
- Denormalised lookups (a table pointing at its parent and at its parent's parent) add triangles of lines.

## Verdict
Spaghetti on any page that holds a hub. Next: stop pulling other pages' children onto a hub's home page.
