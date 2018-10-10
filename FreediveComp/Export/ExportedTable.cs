using MilanWilczak.FreediveComp.Api;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MilanWilczak.FreediveComp.Export
{
    public class ExportedTable
    {
        public ExportedTable()
        {
            Groups = new List<string>();
            Headers = new List<string>();
            Rows = new List<List<string>>();
        }

        public string Title { get; set; }
        public List<string> Groups { get; private set; }
        public List<string> Headers { get; private set; }
        public List<List<string>> Rows { get; private set; }
    }

    public interface ITableWriter
    {
        HttpResponseMessage ExportTable(ExportedTable table);
    }

    public class HtmlTableWriter : ITableWriter
    {
        public HttpResponseMessage ExportTable(ExportedTable table)
        {
            XElement htmlTitle = string.IsNullOrEmpty(table.Title) ? null : new XElement("title", table.Title);
            XElement htmlPageHeader = string.IsNullOrEmpty(table.Title) ? null : new XElement("h1", table.Title);
            XElement htmlTableHead = new XElement("thead");
            if (table.Groups.Count > 0)
            {
                htmlTableHead.Add(BuildDataRow(table.Groups));
            }
            htmlTableHead.Add(BuildDataRow(table.Headers));
            XElement htmlTableBody = new XElement("tbody");
            foreach (var row in table.Rows)
            {
                htmlTableBody.Add(BuildDataRow(row));
            }

            XDocument document = new XDocument(
                new XDocumentType("html", null, null, null),
                new XElement(
                    "html",
                    new XElement("head", htmlTitle),
                    new XElement(
                        "body",
                        htmlPageHeader,
                        new XElement("table", htmlTableHead, htmlTableBody))));

            var xmlSettings = new XmlWriterSettings { CloseOutput = false, Indent = true, IndentChars = "  ", OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.None };
            using (var stream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(stream, xmlSettings))
                {
                    document.WriteTo(xmlWriter);
                }
                var message = new HttpResponseMessage(HttpStatusCode.OK);
                stream.Position = 0;
                message.Content = new StreamContent(stream);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                return message;
            }
        }

        private XElement BuildHeaderRow(List<string> values)
        {
            string pendingTitle = null;
            int pendingCount = 0;
            XElement htmlRow = new XElement("tr");
            foreach (var value in values)
            {
                if (pendingCount == 0)
                {
                    pendingCount = 1;
                    pendingTitle = value;
                }
                else if (value == null || value == pendingTitle)
                {
                    pendingCount++;
                }
                else
                {
                    htmlRow.Add(new XElement(
                        "th",
                        pendingCount > 1 ? new XAttribute("colspan", pendingCount) : null,
                        pendingTitle
                        ));
                    pendingCount = 1;
                    pendingTitle = value;
                }
            }
            if (pendingCount > 0)
            {
                htmlRow.Add(new XElement(
                    "th",
                    pendingCount > 1 ? new XAttribute("colspan", pendingCount) : null,
                    pendingTitle
                    ));
            }
            return htmlRow;
        }

        private XElement BuildDataRow(List<string> values)
        {
            return new XElement("tr", values.Select(value => new XElement("td", value)));
        }
    }

    public class CsvTableWriter : ITableWriter
    {
        public HttpResponseMessage ExportTable(ExportedTable table)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true))
                {
                    ExportRow(writer, table.Headers);
                    foreach (var row in table.Rows)
                    {
                        ExportRow(writer, row);
                    }
                }
                var message = new HttpResponseMessage(HttpStatusCode.OK);
                stream.Position = 0;
                message.Content = new StreamContent(stream);
                message.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                message.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = table.Title + ".csv" };
                return message;
            }
        }

        private void ExportRow(TextWriter writer, List<string> values)
        {
            var separator = "";
            foreach (var value in values)
            {
                writer.Write(separator);
                separator = ";";
                writer.Write(EscapeValue(value));
            }
            writer.WriteLine();
        }

        private string EscapeValue(string input)
        {
            if (!input.Contains(";")) return input;
            return "\"" + input.Replace("\"", "\"\"") + "\"";
        }
    }

    public interface IExportedTableColumn<TInput>
    {
        string Key { get; }
        string Title { get; }
        string Extract(TInput input);
    }

    public abstract class ExportedTableColumnBase<TInput> : IExportedTableColumn<TInput>
    {
        private readonly string key;
        private readonly string title;

        protected ExportedTableColumnBase(string key, string title)
        {
            this.key = key;
            this.title = title;
        }

        public string Key => key;

        public string Title => title;

        public abstract string Extract(TInput input);
    }

    public class ExportedTableColumnManual<TInput> : ExportedTableColumnBase<TInput>
    {
        private readonly Func<TInput, string> extractor;

        public ExportedTableColumnManual(string key, string title, Func<TInput, string> extractor)
            : base(key, title)
        {
            this.extractor = extractor;
        }

        public override string Extract(TInput input)
        {
            return extractor(input) ?? "";
        }
    }

    public static class ExportedTableColumnExtractors
    {
        public static string AthleteFullName(AthleteProfile athlete)
        {
            return athlete.FirstName + " " + athlete.Surname;
        }

        public static string PerformanceDuration(PerformanceDto performance)
        {
            if (performance == null || performance.Duration == null) return "";
            var duration = performance.Duration.Value;
            return string.Format("{0}:{1:00}", duration.Minutes, duration.Seconds);
        }

        public static string PerformanceDistance(PerformanceDto performance)
        {
            if (performance == null || performance.Distance == null) return "";
            var distance = performance.Distance.Value;
            return distance.ToString() + "m";
        }

        public static string PerformanceDepth(PerformanceDto performance)
        {
            if (performance == null || performance.Depth == null) return "";
            var depth = performance.Depth.Value;
            return depth.ToString() + "m";
        }

        public static string PerformancePoints(PerformanceDto performance)
        {
            if (performance == null || performance.Points == null) return "";
            var points = performance.Points.Value;
            return points.ToString() + "p";
        }

        public static string Performance(PerformanceComponent component, PerformanceDto performance)
        {
            if (component == PerformanceComponent.Duration) return PerformanceDuration(performance);
            if (component == PerformanceComponent.Distance) return PerformanceDistance(performance);
            if (component == PerformanceComponent.Depth) return PerformanceDepth(performance);
            if (component == PerformanceComponent.Points) return PerformancePoints(performance);
            return "";
        }

        public static string StartTime(DateTimeOffset? time)
        {
            if (time == null) return "";
            var value = time.Value.ToLocalTime();   // TODO: don't use local time but time of the calling client
            return string.Format("{0}:{1:00}", value.Hour, value.Minute);
        }
    }

    public class ExportedTableColumnPrimaryComponent<TInput> : ExportedTableColumnBase<TInput>
    {
        private readonly IRulesRepository rulesRepository;
        private readonly Func<TInput, string> rulesExtractor;
        private readonly Func<TInput, PerformanceDto> performanceExtractor;

        public ExportedTableColumnPrimaryComponent(string key, string title, IRulesRepository rulesRepository, Func<TInput, string> rulesExtractor, Func<TInput, PerformanceDto> performanceExtractor)
            : base(key, title)
        {
            this.rulesRepository = rulesRepository;
            this.rulesExtractor = rulesExtractor;
            this.performanceExtractor = performanceExtractor;
        }

        public override string Extract(TInput input)
        {
            var rulesName = rulesExtractor(input);
            var performance = performanceExtractor(input);
            var rules = rulesRepository.Get(rulesName);
            if (rules == null) return "";
            return ExportedTableColumnExtractors.Performance(rules.PrimaryComponent, performance);
        }
    }
}