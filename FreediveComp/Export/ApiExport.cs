using MilanWilczak.FreediveComp.Api;
using System.Net.Http;

namespace MilanWilczak.FreediveComp.Export
{
    public interface IApiExport
    {
        HttpResponseMessage ExportStartingList(StartingListReport report, string format, string preset);
        HttpResponseMessage ExportResultsList(ResultsListReport report, string format, string preset);
    }

    public class ApiExport : IApiExport
    {
        private readonly StartingListExporter startingListExporter;
        private readonly ResultsListExporter resultsListExporter;

        public HttpResponseMessage ExportStartingList(StartingListReport report, string format, string preset)
        {
            var formatter = GetTableWriter(format);
            var exporter = startingListExporter.GetExporter(preset);
            return formatter.ExportTable(exporter(report));
        }

        public HttpResponseMessage ExportResultsList(ResultsListReport report, string format, string preset)
        {
            var formatter = GetTableWriter(format);
            var exporter = resultsListExporter.GetExporter(preset);
            return formatter.ExportTable(exporter(report));
        }

        private ITableWriter GetTableWriter(string format)
        {
            if (format == "csv") return new CsvTableWriter();
            return new HtmlTableWriter();
        }
    }
}