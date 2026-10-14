// =============================================================================
// XrmToolBox Plugin Entry Point
//
// To update the icon base64 strings after replacing the PNGs, run in PowerShell:
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-32.png"))
//   [Convert]::ToBase64String([IO.File]::ReadAllBytes("Resources\icon-80.png"))
// =============================================================================

using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace SolutionErdXrmToolBoxPlugin
{
    [Export(typeof(IXrmToolBoxPlugin))]
    [ExportMetadata("Name", "Solution ERD Builder")]
    [ExportMetadata("Description", "Draws readable ERDs of the tables in a Dataverse solution, split into subject-area pages, and exports them to draw.io. Alpha.")]
    [ExportMetadata("SmallImageBase64", "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAEtSURBVFhH7Va7DcIwFKSkZAFDA9SMwgBIfIUo6UhHOiSSigUogAHokWAGFqAjbBF0kR2Fx8fPOAiQfNIpUfLufEls5xUK/4JKe1mq9Tc+h6ilemtI85hJn+qt4QL8RIDWfBcvtseXRM3HAmAAHVCTa4AoFJ0oEP5wPDlwA6AWGmhP00qRerJxDsToEpZjcOb1EnMdUINapYtC4VFfNvAU0mhv+gagSQIE4v3PoQLgaDoHslrqy4YLkDWpdlfjBxvOQ6I29wD0ng422hQ2JjbaFDYmRtp6b9OUP5sbYj1jU8GR3gPReDxrVHRajJkGoJPIgMqQXmfRBfitAKqh4FI1HiCnUaG8C2AKaZIEwLkpXAAX4C4AnaU65roK5L9erWk+B+tGQnqdQYyZBvgmrgSZkKo2rrZwAAAAAElFTkSuQmCC")]
    [ExportMetadata("BigImageBase64", "iVBORw0KGgoAAAANSUhEUgAAAFAAAABQCAYAAACOEfKtAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAALcSURBVHhe7ZxPaNNgGId79OhxB9cIY+jdmxfrxXkQ0asolfbkhiIy7MQJxYvSFhTxbIdueJhTh6IexCIIE0EKgvgP8TB0HcyDispEiLzVSPoltEl/yZc0+T3wHEbL8vZZ6deQfMtkCCGEhM5I8eru0cJcQ4+zh9XjDzzyokaLc6Ymy+rxBx4GBGFAEAYEYUAQBgRhQBAGBLEC7iwtmgcrD0Nx7PSd5Ae8tPjCDIuFJ+8ZEIEBQVIX8POX7+bK2te+/fZjPd0BHyy9NWfuPu/b5ptP6Q747OVyO2K/vlteS3fAoGFAkEQEXKkaudVq9larlm3YvTh14JWugKcmJz6ox/8706Z96ryxYrViDLVqwz9btaypWp/e335x9oDyGSYLQb/KSuwW8NxUwXF8y481Y6s6d2yQd586cLeAYa3C3QLKjOrcsaEz4HBTfrY8dOzMeTVgWO/APeOVGfuxZZYBDJht2B+LchH59znIgL1gQJDUBQxrEWFAj6Y+YNAwIAgDgjAgSCoCytUziRiGRy4/Tn5ATSYvoJGvD40Ur+V0uCV/fbP92IkIGCUMCMKAIAwIwoAgDAjCgCAMCKIloJGvb1C/kPr15OTE8ftnd5jifHlXU33cj/Yvw+iXcJnFmktmVB/3q5Gvb+ysl8lkZGCXU6Io/X86pvk0sKcSsbMeA/qSAUEZEJQBQRkQlAFBGRCUAUG7Btxbvmc+fd2KzOqNpjWoI2DpypLj+TotXHjUO6BcNYsSt0uTOi6JekH+gAwIwIAgvgPKLbjq5pag9bJZxi0guknHi7IVzY7vgHL7mHpLWdB6uUXNLaC8QPV3Ba16D7bvgLKZT93gF7ReNgy6BUQ3Knpx/dfvjtl8B4wCrwGjgAFBGBDEd0AuIuAiwoBgwCjwGjAKGBCEAUEYEIQBQRgQxFPAbUfn2xGjcmza+Q/ErIC50m3H83W6/cTN3gFjpCNgXHQNSAghKeQP4rPLVeufWYsAAAAASUVORK5CYII=")]
    [ExportMetadata("BackgroundColor", "White")]
    [ExportMetadata("PrimaryFontColor", "Black")]
    [ExportMetadata("SecondaryFontColor", "Gray")]
    [ExportMetadata("IsOpenSource", true)]
    public class SolutionErdPlugin : PluginBase, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "xrmtoolbox-erd";
        public string UserName => "HurleySk";

        public string HelpUrl => "https://github.com/HurleySk/xrmtoolbox-erd#readme";

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new SolutionErdControl();
        }
    }
}
