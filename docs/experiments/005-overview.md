# 005 Overview page: one line per pair of areas, spread out, straight lines

Date: 14 Oct 2026

## Problem
The overview page shows each subject area as a box and the lookups between areas as lines with a count. It used the detail pages' layout: layered placement, right-angle lines, every line anchored to the middle of a box header. Lines in both directions between the same two areas were drawn separately. On the synthetic fixture 16 lines left a few anchor points and ran on top of each other for long stretches. A reader couldn't tell which count belonged to which pair of areas.

The crossing count didn't see this, because two lines on top of each other don't cross. So this experiment adds a metric first.

## New metric: overlap
`Overlap` is the length where two different lines run on top of each other. It is reported next to the other counts but kept out of the score, so older runs stay comparable. The CLI's TOTAL line prints it.

The box-hit test was also fixed. It used the segment's bounding box, which is exact for right-angle lines but counts a diagonal line as hitting any box near its bounding box. It now clips the segment to the box (Liang-Barsky). Earlier runs had no diagonal lines, so their numbers don't change.

## Baseline (overview page only)

| Fixture | Lines | Crossings | Overlap | Length |
|---------|-------|-----------|---------|--------|
| project-delivery | 16 | 5 | 6684 | 7427 |
| real-a | 9 | 0 | 3509 | 3914 |
| real-b | 13 | 0 | 6763 | 7595 |

## 005a: one line per pair of areas (changes what is drawn)
Both directions between two areas become one line labelled with the total count. No relationship is lost. The detail pages still show which table looks up which.

| Fixture | Lines | Crossings | Bends | Overlap | Length |
|---------|-------|-----------|-------|---------|--------|
| project-delivery | 16 → 11 | 1 | 28 | 3489 | 5066 |
| real-a | 9 → 5 | 0 | 12 | 695 | 1858 |
| real-b | 13 → 9 | 1 | 28 | 2034 | 3892 |

Better, but the layered layout still sends most lines along one shared trunk on the side of the page.

## 005b: spread the areas out (line set fixed at 005a's)
`OverviewStyle` option (CLI `--overview layered|routed|straight`). Spread placement uses MSAGL's MDS layout (boxes placed by graph distance, overlaps removed, three times the usual node spacing). Two ways to draw the lines were compared:

- **Routed**: right-angle lines that can leave a box from any side, routed from the box centres and cut at the box edges.
- **Straight**: one straight line from centre to centre, cut at the box edges.

| Fixture | Style | Crossings | Bends | Box hits | Overlap | Length |
|---------|-------|-----------|-------|----------|---------|--------|
| project-delivery | layered (005a) | 1 | 28 | 0 | 3489 | 5066 |
| | routed | 2 | 23 | 0 | 1830 | 5343 |
| | straight | 3 | 0 | 0 | 0 | 3437 |
| real-a | layered (005a) | 0 | 12 | 0 | 695 | 1858 |
| | routed | 0 | 8 | 0 | 173 | 1965 |
| | straight | 0 | 0 | 0 | 0 | 998 |
| real-b | layered (005a) | 1 | 28 | 0 | 2034 | 3892 |
| | routed | 1 | 16 | 0 | 969 | 4166 |
| | straight (before 005c) | 0 | 0 | 1 | 0 | |

Routed still shares runs and looks busy. Rejected.

Straight never shares a run, and a straight line between two boxes is the easiest line to follow. It has a couple more crossings on the synthetic fixture, but a crossing of two straight lines is easy to read through. Length is measured as |dx| + |dy|, which overstates a diagonal line by up to 41%, so straight's real length advantage is larger than the table shows.

On real-b one straight line went through the middle box of three in a column.

## 005c: curve around a box instead of through it
A straight line that would pass through another box is rerouted with MSAGL's spline router (padding 10/20) and drawn as a smooth curve, sampled into 24 points.

| Fixture | Crossings | Bends | Box hits | Overlap | Length |
|---------|-----------|-------|----------|---------|--------|
| project-delivery | 3 | 0 | 0 | 0 | 3437 |
| real-a | 0 | 0 | 0 | 0 | 998 |
| real-b | 1 | 23 | 0 | 0 | 2659 |

Only real-b had a blocked line. Its curve adds one crossing. The 23 bends are the sample points of that one curve, not real corners, so the bend count and score overstate it.

## Verdict
Kept: 005a, plus spread placement with straight lines and 005c curves. `SpreadStraight` is the default. The page is taller than the layered one (873x956 against 950x558 on project-delivery), which is fine on a screen and on A4/Letter.

draw.io writes straight lines as `edgeStyle=none` with the exit and entry points taken from the actual line ends, so the file matches the preview.

Tests: `ClustererTests.OverviewDrawsOneLinePerPairOfAreas`, `LayoutTests.OverviewLinesNeverShareARun`, `LayoutTests.DetailPageEdgesAreOrthogonal` (overview lines must be straight), and `MetricsTests` for the new overlap and diagonal box-hit measures.

## Next
The detail pages still share runs. Whole-document overlap after 005: project-delivery 4118, real-a 65087, real-b 31674, almost all on detail pages. That's the next layout target, with the drawn line set held fixed.
