using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using SolutionErd.Layout;

namespace SolutionErd.Writers
{
    /// <summary>
    /// Writes an uncompressed draw.io file: one diagram per page, ER table shapes with a row cell per
    /// column, and orthogonal edges glued to row cells with our routes as waypoints.
    /// </summary>
    public static class DrawioWriter
    {
        const string Font = "fontFamily=" + ErdStyle.FontFamily + ";";

        public static void Write(ErdDocument document, string path)
        {
            File.WriteAllText(path, ToXml(document), new UTF8Encoding(false));
        }

        public static string ToXml(ErdDocument document)
        {
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false) };
            var sb = new StringBuilder();
            using (var w = XmlWriter.Create(sb, settings))
            {
                w.WriteStartElement("mxfile");
                w.WriteAttributeString("host", "SolutionErd");
                for (var p = 0; p < document.Pages.Count; p++)
                {
                    WritePage(w, document.Pages[p], "p" + p);
                }

                w.WriteEndElement();
            }

            return sb.ToString();
        }

        static void WritePage(XmlWriter w, PageLayout page, string pageId)
        {
            w.WriteStartElement("diagram");
            w.WriteAttributeString("id", pageId);
            w.WriteAttributeString("name", page.Name);
            w.WriteStartElement("mxGraphModel");
            w.WriteAttributeString("grid", "1");
            w.WriteAttributeString("gridSize", "10");
            w.WriteAttributeString("guides", "1");
            w.WriteAttributeString("tooltips", "1");
            w.WriteAttributeString("connect", "1");
            w.WriteAttributeString("arrows", "1");
            w.WriteAttributeString("fold", "1");
            w.WriteAttributeString("page", "0");
            w.WriteAttributeString("pageScale", "1");
            w.WriteAttributeString("pageWidth", Num(page.Width));
            w.WriteAttributeString("pageHeight", Num(page.Height));
            w.WriteAttributeString("math", "0");
            w.WriteAttributeString("shadow", "0");
            w.WriteStartElement("root");
            Cell(w, "0", null, null, null);
            w.WriteEndElement();
            Cell(w, "1", "0", null, null);
            w.WriteEndElement();

            foreach (var box in page.Boxes)
            {
                var id = pageId + "-" + box.Id;
                if (box.IsStub)
                {
                    Vertex(w, id, "1", HeaderHtml(box),
                        "rounded=1;arcSize=8;dashed=1;fillColor=#F5F5F5;strokeColor=#999999;fontColor=#333333;align=left;spacingLeft=8;" +
                        "verticalAlign=middle;html=1;whiteSpace=wrap;fontSize=12;" + Font,
                        box.X, box.Y, box.Width, box.Height);
                    continue;
                }

                var fill = box.IsArea ? "fillColor=#FFF2CC;strokeColor=#D6B656;" : "fillColor=#DAE8FC;strokeColor=#6C8EBF;";
                Vertex(w, id, "1", HeaderHtml(box),
                    "shape=table;startSize=" + Num(box.HeaderHeight) + ";container=1;collapsible=0;childLayout=tableLayout;fixedRows=1;" +
                    "rowLines=0;fontStyle=0;align=left;spacingLeft=8;verticalAlign=middle;resizeLast=1;html=1;whiteSpace=wrap;" +
                    fill + "swimlaneFillColor=#FFFFFF;fontColor=#1F2D3D;fontSize=12;" + Font,
                    box.X, box.Y, box.Width, box.Height);

                for (var r = 0; r < box.Rows.Count; r++)
                {
                    var row = box.Rows[r];
                    var rowId = RowId(id, r);
                    Vertex(w, rowId, id, "",
                        "shape=tableRow;horizontal=0;startSize=0;swimlaneHead=0;swimlaneBody=0;fillColor=none;collapsible=0;dropTarget=0;" +
                        "points=[[0,0.5],[1,0.5]];portConstraint=eastwest;top=0;left=0;right=0;bottom=0;html=1;",
                        0, row.Top, box.Width, row.Height);

                    var nameX = 0.0;
                    if (!box.IsArea)
                    {
                        nameX = ErdStyle.KeyColumnWidth;
                        Vertex(w, rowId + "-k", rowId, Html(row.Key ?? ""),
                            "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;fontStyle=1;overflow=hidden;" +
                            "whiteSpace=wrap;html=1;fontSize=9;fontColor=#6C8EBF;" + Font,
                            0, 0, nameX, row.Height, alternateBounds: true);
                    }

                    var text = row.Bold ? "<b>" + Html(row.Text) + "</b>" : Html(row.Text);
                    if (row.SubText != null) text += "&nbsp;&nbsp;<span style=\"font-size:10px;color:#6B7785\">" + Html(row.SubText) + "</span>";
                    Vertex(w, rowId + "-n", rowId, text,
                        "shape=partialRectangle;connectable=0;fillColor=none;top=0;left=0;bottom=0;right=0;align=left;spacingLeft=" +
                        (box.IsArea ? "8" : "0") + ";overflow=hidden;whiteSpace=wrap;html=1;fontSize=11;fontColor=#1F2D3D;" + Font,
                        nameX, 0, box.Width - nameX, row.Height, alternateBounds: true);
                }
            }

            for (var i = 0; i < page.Edges.Count; i++)
            {
                WriteEdge(w, page, page.Edges[i], pageId, pageId + "-e" + i);
            }

            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
        }

        static void WriteEdge(XmlWriter w, PageLayout page, EdgeLayout edge, string pageId, string id)
        {
            var from = page.Box(edge.FromBox);
            var to = page.Box(edge.ToBox);
            var fromRow = from.Rows.FindIndex(r => r.Name == edge.FromRow);
            var toRow = to.Rows.FindIndex(r => r.Name == edge.ToRow);
            var source = fromRow >= 0 ? RowId(pageId + "-" + from.Id, fromRow) : pageId + "-" + from.Id;
            var target = toRow >= 0 ? RowId(pageId + "-" + to.Id, toRow) : pageId + "-" + to.Id;
            var exitY = fromRow >= 0 ? 0.5 : (edge.Points[0].Y - from.Y) / from.Height;
            var entryY = toRow >= 0 ? 0.5 : (edge.Points[edge.Points.Count - 1].Y - to.Y) / to.Height;

            var style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;strokeColor=#4D4D4D;" +
                        "startArrow=" + Arrow(edge.StartMark) + ";startFill=0;endArrow=" + Arrow(edge.EndMark) + ";endFill=0;" +
                        "exitX=" + (edge.FromRight ? "1" : "0") + ";exitY=" + Num(exitY) + ";exitDx=0;exitDy=0;" +
                        "entryX=" + (edge.ToRight ? "1" : "0") + ";entryY=" + Num(entryY) + ";entryDx=0;entryDy=0;" +
                        "fontSize=10;fontStyle=1;labelBackgroundColor=#FFFFFF;" + Font;

            w.WriteStartElement("mxCell");
            w.WriteAttributeString("id", id);
            w.WriteAttributeString("value", edge.Label ?? "");
            w.WriteAttributeString("style", style);
            w.WriteAttributeString("edge", "1");
            w.WriteAttributeString("parent", "1");
            w.WriteAttributeString("source", source);
            w.WriteAttributeString("target", target);
            w.WriteStartElement("mxGeometry");
            w.WriteAttributeString("relative", "1");
            w.WriteAttributeString("as", "geometry");
            if (edge.Points.Count > 2)
            {
                w.WriteStartElement("Array");
                w.WriteAttributeString("as", "points");
                foreach (var p in edge.Points.Skip(1).Take(edge.Points.Count - 2))
                {
                    w.WriteStartElement("mxPoint");
                    w.WriteAttributeString("x", Num(p.X));
                    w.WriteAttributeString("y", Num(p.Y));
                    w.WriteEndElement();
                }

                w.WriteEndElement();
            }

            w.WriteEndElement();
            w.WriteEndElement();
        }

        static string RowId(string boxId, int row) => boxId + "-r" + row;

        static string Arrow(EndMark mark)
        {
            switch (mark)
            {
                case EndMark.One: return "ERmandOne";
                case EndMark.ZeroOrOne: return "ERzeroToOne";
                case EndMark.ZeroOrMany: return "ERzeroToMany";
                default: return "none";
            }
        }

        static string HeaderHtml(BoxLayout box)
        {
            var html = "<b>" + Html(box.Title) + "</b>";
            if (!string.IsNullOrEmpty(box.SubTitle)) html += "<br><span style=\"font-size:10px;color:#6B7785\">" + Html(box.SubTitle) + "</span>";
            return html;
        }

        static string Html(string text) => WebUtility.HtmlEncode(text ?? "");

        static void Cell(XmlWriter w, string id, string parent, string value, string style)
        {
            w.WriteStartElement("mxCell");
            w.WriteAttributeString("id", id);
            if (parent != null) w.WriteAttributeString("parent", parent);
            if (value != null) w.WriteAttributeString("value", value);
            if (style != null) w.WriteAttributeString("style", style);
        }

        static void Vertex(XmlWriter w, string id, string parent, string value, string style,
            double x, double y, double width, double height, bool alternateBounds = false)
        {
            Cell(w, id, parent, value, style);
            w.WriteAttributeString("vertex", "1");
            w.WriteStartElement("mxGeometry");
            if (Math.Abs(x) > 0.001) w.WriteAttributeString("x", Num(x));
            if (Math.Abs(y) > 0.001) w.WriteAttributeString("y", Num(y));
            w.WriteAttributeString("width", Num(width));
            w.WriteAttributeString("height", Num(height));
            w.WriteAttributeString("as", "geometry");
            if (alternateBounds)
            {
                w.WriteStartElement("mxRectangle");
                w.WriteAttributeString("width", Num(width));
                w.WriteAttributeString("height", Num(height));
                w.WriteAttributeString("as", "alternateBounds");
                w.WriteEndElement();
            }

            w.WriteEndElement();
            w.WriteEndElement();
        }

        static string Num(double value) => Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
