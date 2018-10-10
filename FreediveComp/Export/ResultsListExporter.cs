using MilanWilczak.FreediveComp.Api;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MilanWilczak.FreediveComp.Export
{
    public class ResultsListExporter
    {
        private Dictionary<string, IExportedTableColumn<AthleteProfile>> availableAthleteFields = new Dictionary<string, IExportedTableColumn<AthleteProfile>>();
        private Dictionary<string, IExportedTableColumn<ResultsListReportEntrySubresult>> availableResultsFields = new Dictionary<string, IExportedTableColumn<ResultsListReportEntrySubresult>>();

        public ResultsListExporter(IRulesRepository rulesRepository)
        {
            AddAthleteField("Athlete.AthleteId", "ID", e => e.AthleteId);
            AddAthleteField("Athlete.FirstName", "First name", e => e.FirstName);
            AddAthleteField("Athlete.Surname", "Surname", e => e.Surname);
            AddAthleteField("Athlete.FullName", "Athlete", e => ExportedTableColumnExtractors.AthleteFullName(e));
            AddAthleteField("Athlete.Club", "Club", e => e.Club);
            AddAthleteField("Athlete.CountryName", "Country", e => e.CountryName);
            AddAthleteField("Athlete.Sex", "Sex", e => e.Sex);
            AddAthleteField("Athlete.Category", "Category", e => e.Category);
            AddAthleteField("Athlete.ModeratorNotes", "Notes", e => e.ModeratorNotes);

            AddResultsField("Announcement.Duration", "Announced", e => ExportedTableColumnExtractors.PerformanceDuration(e.Announcement.Performance));
            AddResultsField("Announcement.Depth", "Announced", e => ExportedTableColumnExtractors.PerformanceDepth(e.Announcement.Performance));
            AddResultsField("Announcement.Distance", "Announced", e => ExportedTableColumnExtractors.PerformanceDistance(e.Announcement.Performance));
            AddResultsField("Announcement.Points", "Announced", e => ExportedTableColumnExtractors.PerformancePoints(e.Announcement.Performance));
            AddResultsField("Announcement.Combined", "Announced", e => ExportedTableColumnExtractors.PerformanceCombined(e.Announcement.Performance));

            AddResultsField("CurrentResult.Actual.Duration", "Realized", e => ExportedTableColumnExtractors.PerformanceDuration(e.CurrentResult.Performance));
            AddResultsField("CurrentResult.Actual.Depth", "Realized", e => ExportedTableColumnExtractors.PerformanceDepth(e.CurrentResult.Performance));
            AddResultsField("CurrentResult.Actual.Distance", "Realized", e => ExportedTableColumnExtractors.PerformanceDistance(e.CurrentResult.Performance));
            AddResultsField("CurrentResult.Actual.Points", "Realized", e => ExportedTableColumnExtractors.PerformancePoints(e.CurrentResult.Performance));
            AddResultsField("CurrentResult.Actual.Combined", "Realized", e => ExportedTableColumnExtractors.PerformanceCombined(e.CurrentResult.Performance));

            AddResultsField("CurrentResult.Final.Duration", "Realized", e => ExportedTableColumnExtractors.PerformanceDuration(e.CurrentResult.FinalPerformance));
            AddResultsField("CurrentResult.Final.Depth", "Realized", e => ExportedTableColumnExtractors.PerformanceDepth(e.CurrentResult.FinalPerformance));
            AddResultsField("CurrentResult.Final.Distance", "Realized", e => ExportedTableColumnExtractors.PerformanceDistance(e.CurrentResult.FinalPerformance));
            AddResultsField("CurrentResult.Final.Points", "Realized", e => ExportedTableColumnExtractors.PerformancePoints(e.CurrentResult.FinalPerformance));
            AddResultsField("CurrentResult.Final.Combined", "Realized", e => ExportedTableColumnExtractors.PerformanceCombined(e.CurrentResult.FinalPerformance));

            AddResultsField("FinalPoints", "Points", e => ExportedTableColumnExtractors.Points(e.FinalPoints));
            AddResultsField("FinalPointsTotal", "", e => ExportedTableColumnExtractors.Points(e.FinalPoints));
        }

        private void AddAthleteField(string key, string title, Func<AthleteProfile, string> extractor)
        {
            availableAthleteFields[key] = new ExportedTableColumnManual<AthleteProfile>(key, title, extractor);
        }

        private void AddResultsField(string key, string title, Func<ResultsListReportEntrySubresult, string> extractor)
        {
            availableResultsFields[key] = new ExportedTableColumnManual<ResultsListReportEntrySubresult>(key, title, extractor);
        }

        public Func<ResultsListReport, ExportedTable> GetExporter(string preset)
        {
            var exporter = new PreparedResultsListExporter();

            exporter.AddAthleteField(GetAthleteField("Athlete.FullName"));
            exporter.AddAthleteField(GetAthleteField("Athlete.CountryName"));

            exporter.AddResultField(GetResultsField("Announcement.Combined"), true, true);
            exporter.AddResultField(GetResultsField("CurrentResult.Final.Combined"), true, true);
            exporter.AddResultField(GetResultsField("FinalPoints"), true, true);

            exporter.AddResultField(GetResultsField("Announcement.Combined"), true, false);
            exporter.AddResultField(GetResultsField("CurrentResult.Final.Combined"), true, false);

            exporter.AddResultField(GetResultsField("FinalPointsTotal"), false, true);

            return exporter.Export;
        }

        private IExportedTableColumn<AthleteProfile> GetAthleteField(string fieldName)
        {
            availableAthleteFields.TryGetValue(fieldName, out IExportedTableColumn<AthleteProfile> column);
            return column;
        }

        private IExportedTableColumn<ResultsListReportEntrySubresult> GetResultsField(string fieldName)
        {
            availableResultsFields.TryGetValue(fieldName, out IExportedTableColumn<ResultsListReportEntrySubresult> column);
            return column;
        }
    }

    public class PreparedResultsListExporter
    {
        private readonly List<IExportedTableColumn<AthleteProfile>> athleteFields = new List<IExportedTableColumn<AthleteProfile>>();
        private readonly List<IExportedTableColumn<ResultsListReportEntrySubresult>> performanceOnlyFields = new List<IExportedTableColumn<ResultsListReportEntrySubresult>>();
        private readonly List<IExportedTableColumn<ResultsListReportEntrySubresult>> pointsOnlyFields = new List<IExportedTableColumn<ResultsListReportEntrySubresult>>();
        private readonly List<IExportedTableColumn<ResultsListReportEntrySubresult>> fullResultFields = new List<IExportedTableColumn<ResultsListReportEntrySubresult>>();

        public void AddAthleteField(IExportedTableColumn<AthleteProfile> field)
        {
            if (field != null) athleteFields.Add(field);
        }

        public void AddResultField(IExportedTableColumn<ResultsListReportEntrySubresult> field, bool hasPerformance, bool hasPoints)
        {
            var fieldSet = GetFieldSet(hasPerformance, hasPoints);
            if (fieldSet != null && field != null) fieldSet.Add(field);
        }

        public ExportedTable Export(ResultsListReport report)
        {
            var table = new ExportedTable();
            table.Title = report.Metadata.Title;
            foreach (var athleteField in athleteFields)
            {
                table.Groups.Add("Athlete");
                table.Headers.Add(athleteField.Title);
            }
            var fieldSets = new List<List<IExportedTableColumn<ResultsListReportEntrySubresult>>>();
            foreach (var reportColumn in report.Metadata.Columns)
            {
                var fieldSet = GetFieldSet(reportColumn.HasPerformance, reportColumn.HasFinalPoints);
                if (fieldSet == null) continue;
                foreach (var field in fieldSet)
                {
                    table.Groups.Add(reportColumn.Title);
                    table.Headers.Add(field.Title);
                }
                fieldSets.Add(fieldSet);
            }
            foreach (var entry in report.Results)
            {
                var row = new List<string>();
                foreach (var athleteField in athleteFields)
                {
                    row.Add(athleteField.Extract(entry.Athlete));
                }
                for (var i = 0; i < fieldSets.Count; i++)
                {
                    var subresult = entry.Subresults[i];
                    var fieldSet = fieldSets[i];
                    foreach (var field in fieldSet)
                    {
                        row.Add(field.Extract(subresult));
                    }
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private List<IExportedTableColumn<ResultsListReportEntrySubresult>> GetFieldSet(bool hasPerformance, bool hasPoints)
        {
            if (hasPerformance)
            {
                return hasPoints ? fullResultFields : performanceOnlyFields;
            }
            else
            {
                return hasPoints ? pointsOnlyFields : null;
            }
        }

    }
}