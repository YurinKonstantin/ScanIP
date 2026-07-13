using ScanIPPro.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ScanIPPro.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

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
            string fileName = null)
        {
            try
            {
                // Выбор места сохранения
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.SuggestedFileName = fileName ?? $"ScanResult_{DateTime.Now:yyyyMMdd_HHmmss}";

                switch (format)
                {
                    case ExportFormat.Csv:
                        picker.FileTypeChoices.Add("CSV файл", new List<string> { ".csv" });
                        break;
                    case ExportFormat.Json:
                        picker.FileTypeChoices.Add("JSON файл", new List<string> { ".json" });
                        break;
                    case ExportFormat.Txt:
                        picker.FileTypeChoices.Add("Текстовый файл", new List<string> { ".txt" });
                        break;
                    case ExportFormat.Html:
                        picker.FileTypeChoices.Add("HTML файл", new List<string> { ".html" });
                        break;
                }

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

        private string ExportToCsv(IEnumerable<ScanResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("IP-адрес;Имя хоста;MAC-адрес;Производитель;Открытые порты;Время отклика;Статус");

            foreach (var item in results)
            {
                sb.AppendLine($"{item.IPAddress};{item.HostName};{item.MACAddress};{item.Vendor};{item.OpenPorts};{item.RoundtripTime};{item.Status}");
            }

            return sb.ToString();
        }

        private string ExportToJson(IEnumerable<ScanResult> results)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(results, options);
        }

        private string ExportToTxt(IEnumerable<ScanResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("============ SCAN IP PRO ============");
            sb.AppendLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Всего устройств: {results.Count()}");
            sb.AppendLine($"Онлайн: {results.Count(r => r.IsOnline)}");
            sb.AppendLine("======================================");
            sb.AppendLine();

            foreach (var item in results)
            {
                sb.AppendLine($"IP: {item.IPAddress}");
                sb.AppendLine($"Имя: {item.HostName}");
                sb.AppendLine($"MAC: {item.MACAddress}");
                sb.AppendLine($"Производитель: {item.Vendor}");
                sb.AppendLine($"Порты: {item.OpenPorts}");
                sb.AppendLine($"Отклик: {item.RoundtripTime} мс");
                sb.AppendLine($"Статус: {item.Status}");
                sb.AppendLine(new string('-', 30));
            }

            return sb.ToString();
        }

        private string ExportToHtml(IEnumerable<ScanResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>ScanIP Pro - Результаты сканирования</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #4CAF50; color: white; }");
            sb.AppendLine(".online { color: green; font-weight: bold; }");
            sb.AppendLine(".offline { color: gray; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<h1>ScanIP Pro - Результаты сканирования</h1>");
            sb.AppendLine($"<p>Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine($"<p>Всего устройств: {results.Count()}, Онлайн: {results.Count(r => r.IsOnline)}</p>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>IP-адрес</th><th>Имя хоста</th><th>MAC-адрес</th><th>Производитель</th><th>Открытые порты</th><th>Время отклика</th><th>Статус</th></tr>");

            foreach (var item in results)
            {
                var statusClass = item.IsOnline ? "online" : "offline";
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{item.IPAddress}</td>");
                sb.AppendLine($"<td>{item.HostName}</td>");
                sb.AppendLine($"<td>{item.MACAddress}</td>");
                sb.AppendLine($"<td>{item.Vendor}</td>");
                sb.AppendLine($"<td>{item.OpenPorts}</td>");
                sb.AppendLine($"<td>{item.RoundtripTime} мс</td>");
                sb.AppendLine($"<td class='{statusClass}'>{item.Status}</td>");
                sb.AppendLine($"</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
