# 002 A page draws only its own tables' lookups

Date: 14 Oct 2026

## Change
A split page used to draw every relationship touching its tables. Now it draws the lookups *from* its tables. Lookups from tables on other pages into this page are drawn on their own pages. N:N lines are still drawn on both pages. `--incoming` (`ShowIncomingFromOtherPages`) brings back the old behaviour.

## Results

| Fixture | Edges | Crossings | Bends | Score | vs 001 |
|---------|-------|-----------|-------|-------|--------|
| project-delivery | 133 | 80 | 296 | 813.7 | crossings -20% |
| real-a | 240 | 1013 | 605 | 4886.1 | crossings +3% |
| real-b | 250 | 710 | 600 | 3543.0 | crossings -31% |

## Notes
- The synthetic Project page went from 34 boxes (24 stubs) to 19.
- real-a hardly moved. `solution-erd stats` shows why: its two dense pages are 74 and 61 lines between full tables on the page, plus 24 and 34 lines to stubs. Almost none came in from other pages.
- real-b's biggest page still had 49 stubs taking 79 of its 108 lines.

## Verdict
Kept (it is also the right rule: a table's lookups belong with the table). Not enough on its own; the stub lines are next.
