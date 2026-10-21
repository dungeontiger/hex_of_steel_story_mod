using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Story.Core;

/// <summary>Uses locally installed Windows fonts; proprietary font files are never distributed with the mod.</summary>
public sealed class WindowsFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        return File.ReadAllBytes(Path.Combine(root, faceName == "story-bold" ? "timesbd.ttf" : "times.ttf"));
    }
    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new FontResolverInfo(bold ? "story-bold" : "story-regular");
}

public static class HistoryPdf
{
    private static readonly object Gate = new object();
    public static void Write(string path, string[] paragraphs, string? logoPath = null)
    {
        lock (Gate)
        {
            if (GlobalFontSettings.FontResolver == null) GlobalFontSettings.FontResolver = new WindowsFontResolver();
            using var document = new PdfDocument();
            document.Info.Title = paragraphs.Length == 0 ? "Campaign history" : paragraphs[0];
            document.Info.Author = "Stephen D. Gibson / King Tiger Books";
            var normal = new XFont("StorySerif", 11);
            var heading = new XFont("StorySerif", 22, XFontStyleEx.Bold);
            var small = new XFont("StorySerif", 9);
            XGraphics? graphics = null;
            double y = 0;
            void Page()
            {
                graphics?.Dispose();
                var page = document.AddPage(); page.Size = PdfSharp.PageSize.A4;
                graphics = XGraphics.FromPdfPage(page);
                graphics.DrawString("KING TIGER BOOKS", small, XBrushes.DarkGoldenrod, 48, 35);
                graphics.DrawLine(XPens.DarkGoldenrod, 48, 44, 547, 44);
                graphics.DrawString("Copyright (c) 2026 Stephen D. Gibson | " + document.PageCount, small, XBrushes.Gray, 48, 810);
                y = 72;
            }
            Page();
            if (logoPath != null && File.Exists(logoPath))
            {
                using var logo = XImage.FromFile(logoPath);
                graphics!.DrawImage(logo, 400, y - 12, 147, 147.0 * logo.PixelHeight / logo.PixelWidth);
                y += 48;
            }
            for (int p = 0; p < paragraphs.Length; p++)
            {
                var font = p == 0 ? heading : normal;
                double lineHeight = p == 0 ? 27 : 16;
                string line = "";
                void DrawLine()
                {
                    if (y > 775) Page();
                    graphics!.DrawString(line, font, XBrushes.Black, 48, y);
                    y += lineHeight; line = "";
                }
                foreach (var word in paragraphs[p].Replace("\r", " ").Replace("\n", " ").Split(' '))
                {
                    if (word.Length == 0) continue;
                    if (line.Length > 0 && graphics!.MeasureString(line + " " + word, font).Width > 499) DrawLine();
                    // Split unusually long names/identifiers so user-authored text cannot overflow the page.
                    foreach (char c in word)
                    {
                        if (graphics!.MeasureString(line + c, font).Width > 499) DrawLine();
                        line += c;
                    }
                    line += " ";
                }
                if (line.Length > 0) DrawLine();
                y += 9;
            }
            graphics?.Dispose();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            var temp = path + ".tmp";
            try { document.Save(temp); if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
