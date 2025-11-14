using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Drawing.Imaging;

namespace MenuBuPrinterAgent.Printing;

internal static class ReceiptHtmlRenderer
{
    public static string Render(PrintContent content, string printerWidth)
    {
        if (content.Lines.Count == 0 && string.IsNullOrWhiteSpace(content.Html))
        {
            throw new InvalidOperationException("Yazdırılacak içerik bulunamadı.");
        }

        var widthToken = printerWidth.StartsWith("80", StringComparison.OrdinalIgnoreCase) ? "80mm" : "58mm";
        var printableWidth = printerWidth.StartsWith("80", StringComparison.OrdinalIgnoreCase) ? "72mm" : "52mm";
        var padding = printerWidth.StartsWith("80", StringComparison.OrdinalIgnoreCase) ? "4mm" : "3mm";
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"tr\"><head><meta charset=\"UTF-8\" />");
        sb.Append("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<style>");
        sb.Append(GetBaseStyles(widthToken, printableWidth, padding));
        sb.Append("</style></head><body><div class=\"ticket\">");

        foreach (var line in content.Lines)
        {
            switch (line.Kind)
            {
                case PrintLineKind.Separator:
                    sb.Append("<div class=\"separator\"></div>");
                    break;
                case PrintLineKind.Spacer:
                    sb.Append($"<div class=\"spacer\" style=\"height:{ToMillimeters(line.CustomSpacing ?? 4f)}\"></div>");
                    break;
                case PrintLineKind.Columns:
                    AppendColumns(sb, line);
                    break;
                default:
                    AppendTextLine(sb, line);
                    break;
            }
        }

        var qr = TryConvertQr(content);
        if (!string.IsNullOrEmpty(qr))
        {
            sb.Append("<div class=\"qr\"><img alt=\"QR\" src=\"data:image/png;base64,");
            sb.Append(qr);
            sb.Append("\" /></div>");
        }

        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    private static void AppendTextLine(StringBuilder sb, PrintLine line)
    {
        var classes = $"line {GetStyleClass(line.Style)} {GetAlignmentClass(line.Alignment)}";
        var spacing = line.CustomSpacing.HasValue ? $" style=\"margin-bottom:{ToMillimeters(line.CustomSpacing.Value)}\"" : string.Empty;
        sb.Append("<div class=\"").Append(classes).Append("\"").Append(spacing).Append(">");
        sb.Append(WebUtility.HtmlEncode(line.Text));
        sb.Append("</div>");
    }

    private static void AppendColumns(StringBuilder sb, PrintLine line)
    {
        if (line.Columns is not { Count: > 0 })
        {
            return;
        }

        var weights = line.Columns.Select(c => c.WidthFraction ?? 0f).ToArray();
        var total = weights.Sum();
        if (total <= 0)
        {
            for (var i = 0; i < weights.Length; i++)
            {
                weights[i] = 1f / weights.Length;
            }
        }

        sb.Append("<div class=\"columns\">");
        for (var i = 0; i < line.Columns.Count; i++)
        {
            var column = line.Columns[i];
            var widthPercent = total > 0 ? (weights[i] / total) * 100 : (100f / line.Columns.Count);
            sb.Append("<div class=\"col ")
                .Append(GetStyleClass(column.Style))
                .Append(' ')
                .Append(GetAlignmentClass(column.Alignment))
                .Append("\" style=\"width:")
                .Append(widthPercent.ToString("0.##", CultureInfo.InvariantCulture))
                .Append("%\">")
                .Append(WebUtility.HtmlEncode(column.Text))
                .Append("</div>");
        }
        sb.Append("</div>");
    }

    private static string GetStyleClass(PrintLineStyle style) => style switch
    {
        PrintLineStyle.Bold => "bold",
        PrintLineStyle.Small => "small",
        _ => "normal"
    };

    private static string GetAlignmentClass(PrintLineAlignment alignment) => alignment switch
    {
        PrintLineAlignment.Center => "center",
        PrintLineAlignment.Right => "right",
        _ => "left"
    };

    private static string ToMillimeters(float value)
    {
        var mm = Math.Clamp(value / 6f, 0.5f, 15f);
        return mm.ToString("0.##", CultureInfo.InvariantCulture) + "mm";
    }

    private static string? TryConvertQr(PrintContent content)
    {
        if (content.QrImage == null)
        {
            return null;
        }

        try
        {
            using var ms = new MemoryStream();
            content.QrImage.Save(ms, ImageFormat.Png);
            content.QrImage.Dispose();
            content.QrImage = null;
            return Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            return null;
        }
    }

    private static string GetBaseStyles(string ticketWidth, string printableWidth, string padding) => $@"
:root {{
  --ticket-width: {ticketWidth};
  --printable-width: {printableWidth};
  --ticket-padding: {padding};
  --text-color: #000;
}}

html, body {{
  margin: 0;
  padding: 0;
  width: var(--ticket-width);
  font-family: 'Segoe UI', 'Arial', sans-serif;
  background: #fff;
  color: var(--text-color);
}}

.ticket {{
  width: var(--printable-width);
  margin: 0 auto;
  padding: var(--ticket-padding);
  font-size: 12px;
  line-height: 1.35;
}}

.line {{
  margin: 2px 0;
  word-break: break-word;
}}

.line.bold {{ font-weight: 600; }}
.line.small {{ font-size: 11px; }}
.line.center {{ text-align: center; }}
.line.right {{ text-align: right; }}
.line.left {{ text-align: left; }}

.separator {{
  border-top: 1px dashed #000;
  margin: 6px 0;
}}

.spacer {{
  width: 100%;
}}

.columns {{
  display: flex;
  width: 100%;
  gap: 4px;
}}

.columns .col {{
  word-break: break-word;
}}

.qr {{
  text-align: center;
  margin-top: 8px;
}}

.qr img {{
  width: 65%;
  max-width: 230px;
}}
";
}
