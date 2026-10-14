# Solution ERD Builder

An [XrmToolBox](https://www.xrmtoolbox.com/) tool that draws entity relationship diagrams of the tables in a Dataverse solution, and exports them to draw.io (Visio .vsdx is coming next).

Most ERD tools draw every relationship of every table on one canvas, and the result is unreadable. This tool is built around readability:

- **Noise filtering.** System relationships (created by, modified by, owner, owning team or business unit, currency, process stage and similar) are hidden by default.
- **Subject-area pages.** Large solutions are split into pages of closely related tables, plus an overview page. Hub tables such as account or contact get one home page and appear as small stubs on the others.
- **Lines that start at the lookup.** Each relationship line leaves from the lookup column's row and ends at the related table's primary key, with crow's-foot ends.
- **Layered layout with orthogonal routing**, using [MSAGL](https://github.com/microsoft/automatic-graph-layout).

## Status

Early development; not in the Tool Library yet.

| Part | State |
|------|-------|
| Layout engine, noise filter, subject-area pages | working, being tuned |
| draw.io export | working |
| XrmToolBox UI (solution picker, preview, export) | in progress |
| Visio (.vsdx) export | planned |

## Installation

### From the XrmToolBox Tool Library
Not published yet.

### Manual installation
1. Build: `.\build.ps1`
2. Deploy to XrmToolBox: `.\deploy.ps1 -Force`

## Repository layout

| Path | Contents |
|------|----------|
| `src/SolutionErd.Core` | metadata snapshot model, noise filter, page clustering, layout, preview renderer, writers. No XrmToolBox references. |
| `src/SolutionErdXrmToolBoxPlugin` | the XrmToolBox tool: reads metadata over the connection, shows a preview, exports |
| `src/SolutionErd.Cli` | developer tool: renders a saved metadata snapshot to .drawio and PNG, and prints layout metrics |
| `tests/SolutionErd.Tests` | unit and layout tests |
| `fixtures/` | synthetic metadata snapshots used by the tests |
| `docs/experiments/` | layout experiment log |

## Building from source

```powershell
git clone https://github.com/HurleySk/xrmtoolbox-erd.git
cd xrmtoolbox-erd
dotnet build SolutionErd.sln --configuration Release
dotnet test SolutionErd.sln
```

### Rendering a snapshot without XrmToolBox

The tool can save the metadata it reads as a JSON snapshot. The CLI renders a snapshot the same way the tool does:

```powershell
src\SolutionErd.Cli\bin\Release\net48\solution-erd.exe render fixtures\synthetic\field-service.json out
```

### Testing in XrmToolBox

1. `.\deploy.ps1 -Force` (closes XrmToolBox if it is running, builds, deploys and relaunches)
2. If the tool doesn't appear, delete `%APPDATA%\MscrmTools\XrmToolBox\Plugins\manifest.json` and restart XrmToolBox.

Deploy script options:
- `-SkipBuild` - use existing binaries
- `-Force` - close XrmToolBox automatically if it is running
- `-WhatIf` - show what would happen without making changes

### Create the NuGet package

```powershell
dotnet pack src\SolutionErdXrmToolBoxPlugin --configuration Release
```

The plugin DLL goes in `Plugins/`; `SolutionErd.Core.dll` and `AutomaticGraphLayout.dll` go in `Plugins/Dependencies/`.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Author

**Samuel Hurley** - [HurleySk](https://github.com/HurleySk)

Built on [XrmToolBox](https://www.xrmtoolbox.com/) by Tanguy Touzard.
