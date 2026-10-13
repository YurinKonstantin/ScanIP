using ScanIPPro.Models;
using ScanIPPro.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ScanIPPro.Services
{
    public class ExportService
    {
        public enum ExportFormat
        {
            Csv,
            Json,
            Txt,
            Html
        }

        public async Task<bool> ExportAsync(
            IEnumerable<ScanResult> results,
            ExportFormat format,
            IntPtr windowHandle,
            string fileName = null)
        {
            try
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = fileName ?? $"ScanResult_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                switch (format)
                {
                    case ExportFormat.Csv:
                        picker.FileTypeChoices.Add(LocalizationService.GetString("Export.Csv"), new List<string> { ".csv" });
                        break;
                    case ExportFormat.Json:
                        picker.FileTypeChoices.Add(LocalizationService.GetString("Export.Json"), new List<string> { ".json" });
                        break;
                    case ExportFormat.Txt:
                        picker.FileTypeChoices.Add(LocalizationService.GetString("Export.Txt"), new List<string> { ".txt" });
                        break;
                    case ExportFormat.Html:
                        picker.FileTypeChoices.Add(LocalizationService.GetString("Export.Html"), new List<string> { ".html" });
                        break;
                }

                InitializeWithWindow.Initialize(picker, windowHandle);

                var file = await picker.PickSaveFileAsync();
                if (file == null)
                    return false;

                var content = format switch
                {
                    ExportFormat.Csv => ExportToCsv(results),
                    ExportFormat.Json => ExportToJson(results),
                    ExportFormat.Txt => ExportToTxt(results),
                    ExportFormat.Html => ExportToHtml(results),
                    _ => throw new NotSupportedException()
                };

                await FileIO.WriteTextAsync(file, content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ExportToCsv(IEnumerable<ScanResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine(LocalizationService.GetString("Export.Columns"));

            foreach (var item in results)
            {
                sb.AppendLine($"{item.IPAddress};{item.HostName};{item.MACAddress};{item.Vendor};{item.OpenPorts};{item.RoundtripTime};{item.DisplayStatus}");
            }

            return sb.ToString();
        }

        private static string ExportToJson(IEnumerable<ScanResult> results)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(results, options);
        }

        private static string ExportToTxt(IEnumerable<ScanResult> results)
        {
            var list = results.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("============ SCAN IP PRO ============");
            sb.AppendLine($"{LocalizationService.GetString("Export.Date")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"{LocalizationService.GetString("Export.TotalDevices")}: {list.Count}");
            sb.AppendLine($"{LocalizationService.GetString("Export.Online")}: {list.Count(r => r.IsOnline)}");
            sb.AppendLine("======================================");
            sb.AppendLine();

            foreach (var item in list)
            {
                sb.AppendLine($"IP: {item.IPAddress}");
                sb.AppendLine($"{LocalizationService.GetString("Column.Host")}: {item.HostName}");
                sb.AppendLine($"MAC: {item.MACAddress}");
                sb.AppendLine($"{LocalizationService.GetString("Column.Vendor")}: {item.Vendor}");
                sb.AppendLine($"{LocalizationService.GetString("Column.Ports")}: {item.OpenPorts}");
                sb.AppendLine($"{LocalizationService.GetString("Column.RTT")}: {LocalizationService.GetString("Unit.Milliseconds", item.RoundtripTime)}");
                sb.AppendLine($"{LocalizationService.GetString("Column.Status")}: {item.DisplayStatus}");
                sb.AppendLine(new string('-', 30));
            }

            return sb.ToString();
        }

        private static string ExportToHtml(IEnumerable<ScanResult> results)
        {
            var list = results.ToList();
            var sb = new StringBuilder();
            var title = LocalizationService.GetString("Export.ReportTitle");

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='UTF-8'>");
            sb.AppendLine($"<title>{title}</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:20px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ddd;padding:8px}th{background:#0078D4;color:#fff}.online{color:#107c10;font-weight:bold}.offline{color:#666}</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine($"<h1>{title}</h1>");
            sb.AppendLine($"<p>{LocalizationService.GetString("Export.Date")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"<p>{LocalizationService.GetString("Export.TotalDevices")}: {list.Count}, {LocalizationService.GetString("Export.Online")}: {list.Count(r => r.IsOnline)}</p>");
            sb.AppendLine("<table><tr>");
            foreach (var col in LocalizationService.GetString("Export.Columns").Split(';'))
                sb.AppendLine($"<th>{col}</th>");
            sb.AppendLine("</tr>");

            foreach (var item in list)
            {
                var statusClass = item.IsOnline ? "online" : "offline";
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{item.IPAddress}</td><td>{item.HostName}</td><td>{item.MACAddress}</td><td>{item.Vendor}</td><td>{item.OpenPorts}</td><td>{item.RoundtripTime}</td><td class='{statusClass}'>{item.DisplayStatus}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }
    }
}
