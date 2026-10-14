# 003 Off-page lookups as row labels

Date: 14 Oct 2026

## Change
A lookup whose target is not drawn in full on the page (outside the solution, or on another page) is no longer a stub box plus a line. The lookup row gets "→ Target" text on its right instead. N:N relationships to off-page tables keep their stubs. `--stubs` (`OffPageLinks.Stubs`) brings back the boxes.

## Results

| Fixture | Edges | Crossings | Bends | Score | vs 002 |
|---------|-------|-----------|-------|-------|--------|
| assets | 9 | 1 | 18 | 34.3 | |
| project-delivery | 81 | 26 | 186 | 429.8 | crossings -68% |
| real-a | 163 | 481 | 436 | 2842.0 | crossings -53% |
| real-b | 105 | 156 | 274 | 1025.7 | crossings -78% |

## Notes
- real-b's biggest page lost all 49 stubs.
- What's left on real-a is denormalisation: ten component tables each look up their structure table and four of its ancestors, about 50 lines into 5 tables. The structure table already has those same lookups.

## Verdict
Kept, default on. The row text keeps the information, and pages are much smaller.
