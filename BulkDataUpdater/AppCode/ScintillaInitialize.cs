using ScintillaNET;
using System;
using System.Drawing;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public static class ScintillaInitialize
    {
        public static void InitXML(Scintilla scintilla, bool format)
        {
            scintilla.StyleResetDefault();
            scintilla.Styles[Style.Default].Font = "Consolas";
            scintilla.Styles[Style.Default].Size = 10;
            scintilla.StyleClearAll();

            // XML
            scintilla.Styles[Style.Xml.Asp].BackColor = Color.Yellow;
            scintilla.Styles[Style.Xml.AspAt].ForeColor = Color.Black;
            scintilla.Styles[Style.Xml.AspAt].BackColor = Color.Yellow;
            scintilla.Styles[Style.Xml.AttributeUnknown].ForeColor = Color.Red;
            scintilla.Styles[Style.Xml.Attribute].ForeColor = Color.Red;
            scintilla.Styles[Style.Xml.CData].ForeColor = Color.Blue;
            scintilla.Styles[Style.Xml.Comment].ForeColor = Color.Green;
            scintilla.Styles[Style.Xml.Default].ForeColor = Color.Black;
            scintilla.Styles[Style.Xml.DoubleString].ForeColor = Color.Blue;

            scintilla.Styles[Style.Xml.Entity].ForeColor = Color.Blue;
            scintilla.Styles[Style.Xml.Number].ForeColor = Color.Blue;

            scintilla.Styles[Style.Html.Other].ForeColor = Color.FromArgb(128, 0, 0);
            scintilla.Styles[Style.Html.Script].ForeColor = Color.FromArgb(128, 0, 0);
            scintilla.Styles[Style.Html.SingleString].ForeColor = Color.Blue;
            scintilla.Styles[Style.Html.Tag].ForeColor = Color.FromArgb(128, 0, 0);
            scintilla.Styles[Style.Html.TagUnknown].ForeColor = Color.FromArgb(128, 0, 0);
            scintilla.Styles[Style.Html.TagEnd].ForeColor = Color.FromArgb(128, 0, 0);
            scintilla.Styles[Style.Html.XcComment].ForeColor = Color.Green;
            scintilla.Styles[Style.Html.XmlStart].ForeColor = Color.Blue;
            scintilla.Styles[Style.Html.XmlEnd].ForeColor = Color.Blue;

            scintilla.Lexer = Lexer.Xml;
            // End XML

            // Instruct the lexer to calculate folding
            scintilla.SetProperty("fold", "1");
            scintilla.SetProperty("fold.compact", "1");
            scintilla.SetProperty("fold.html", "1");

            // Configure a margin to display folding symbols
            scintilla.Margins[2].Type = MarginType.Symbol;
            scintilla.Margins[2].Mask = Marker.MaskFolders;
            scintilla.Margins[2].Sensitive = true;
            scintilla.Margins[2].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                scintilla.Markers[i].SetForeColor(SystemColors.ControlLightLight);
                scintilla.Markers[i].SetBackColor(SystemColors.ControlDark);
            }

            // Configure folding markers with respective symbols
            scintilla.Markers[Marker.Folder].Symbol = MarkerSymbol.BoxPlus;
            scintilla.Markers[Marker.FolderOpen].Symbol = MarkerSymbol.BoxMinus;
            scintilla.Markers[Marker.FolderEnd].Symbol = MarkerSymbol.BoxPlusConnected;
            scintilla.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            scintilla.Markers[Marker.FolderOpenMid].Symbol = MarkerSymbol.BoxMinusConnected;
            scintilla.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            scintilla.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            if (format)
            {
                Format(scintilla);
            }
        }

        public static void Format(Scintilla scintilla)
        {
            var txt = scintilla.Text.Replace("\n", "");
            txt = txt.Replace("\r", "");
            while (txt.Contains("> "))
            {
                txt = txt.Replace("> ", ">");
            }
            txt = txt.Replace("><", ">\n<");
            scintilla.Text = txt;        }
    }
}
