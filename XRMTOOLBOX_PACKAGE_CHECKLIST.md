# XRM ToolBox NuGet Package Checklist

| Requirement | Status |
|------------|--------|
| Assembly in `Plugins/` folder (no framework subfolders) | ✅ |
| DLL filename matches package ID | ✅ |
| Valid Package ID | ✅ |
| Version number | ✅ |
| Authors field | ✅ |
| Description | ✅ |
| Tags include "XrmToolBox" | ✅ |
| Project URL valid and publicly accessible | ✅ |
| Icon URL valid and publicly accessible | ✅ |
| LogoUrl provided and publicly accessible | n/a (iconUrl is used) |
| Repository URL | ✅ |
| PackageIcon included | ✅ |
| Icon is 128x128 PNG | ✅ |
| Inherits from PluginControlBase | ✅ |
| MEF Export attribute | ✅ |
| Plugin metadata attributes | ✅ |
| Uses ExecuteMethod() | ✅ |
| Uses WorkAsync() | ✅ |
| References XrmToolBoxPackage | ✅ |
| References MscrmTools.Xrm.Connection | ✅ |
| README.md included | ✅ |
| License specified | ✅ |
| Code committed | ✅ |
| Code pushed to GitHub | ✅ |
| Repository is PUBLIC | ✅ |
| Package built (Release) | ✅ |
| Dependency DLLs in `Plugins/Dependencies/` | ✅ |
| Version has four parts, last part not zero | ✅ |
| Loads and draws in XrmToolBox 1.2026.9.78 from the package layout | ✅ |
