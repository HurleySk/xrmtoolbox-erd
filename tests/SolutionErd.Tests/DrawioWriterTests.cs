using System.Linq;
using System.Xml.Linq;
using SolutionErd.Writers;
using Xunit;

namespace SolutionErd.Tests
{
    public class DrawioWriterTests
    {
        [Fact]
        public void WritesOneDiagramPerPage()
        {
            var document = ErdBuilder.Build(Fixtures.Synthetic("project-delivery"), new ErdOptions());

            var xml = XDocument.Parse(DrawioWriter.ToXml(document));

            Assert.Equal(document.Pages.Select(p => p.Name), xml.Root.Elements("diagram").Select(d => (string)d.Attribute("name")));
        }

        [Fact]
        public void EdgesAreGluedToCellsThatExist()
        {
            var document = ErdBuilder.Build(Fixtures.Synthetic("project-delivery"), new ErdOptions());

            var xml = XDocument.Parse(DrawioWriter.ToXml(document));

            foreach (var diagram in xml.Root.Elements("diagram"))
            {
                var cells = diagram.Descendants("mxCell").ToList();
                var ids = cells.Select(c => (string)c.Attribute("id")).ToList();
                Assert.Equal(ids.Count, ids.Distinct().Count());
                var idSet = ids.ToHashSet();
                foreach (var edge in cells.Where(c => (string)c.Attribute("edge") == "1"))
                {
                    Assert.Contains((string)edge.Attribute("source"), idSet);
                    Assert.Contains((string)edge.Attribute("target"), idSet);
                }
            }
        }

        [Fact]
        public void LookupEdgesStartAtTheLookupRowCell()
        {
            var document = ErdBuilder.Build(Fixtures.Synthetic("assets"), new ErdOptions());

            var xml = XDocument.Parse(DrawioWriter.ToXml(document));

            var edges = xml.Descendants("mxCell").Where(c => (string)c.Attribute("edge") == "1").ToList();
            Assert.NotEmpty(edges);
            Assert.All(edges, e => Assert.Matches(@"-r\d+$", (string)e.Attribute("source")));
            Assert.All(edges, e => Assert.Contains("startArrow=ERzeroToMany", (string)e.Attribute("style")));
        }
    }
}
