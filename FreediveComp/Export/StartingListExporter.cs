using MilanWilczak.FreediveComp.Api;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Export
{
    public interface IStartingListExporter
    {
        Func<StartingListReport, ExportedTable> GetExporter(string preset);
    }

    public class StartingListExporter : IStartingListExporter
    {
        private readonly Dictionary<string, IExportedTableColumn<StartingListReportEntry>> availableColumns;
        private readonly PreparedStartingListExporter minimal;
        private readonly PreparedStartingListExporter running;

        public StartingListExporter(IRulesRepository rulesRepository)
        {
            availableColumns = new Dictionary<string, IExportedTableColumn<StartingListReportEntry>>();

            AddColumn("Athlete.AthleteId", "ID", e => e.Athlete.AthleteId);
            AddColumn("Athlete.FirstName", "First name", e => e.Athlete.FirstName);
            AddColumn("Athlete.Surname", "Surname", e => e.Athlete.Surname);
            AddColumn("Athlete.FullName", "Athlete", e => ExportedTableColumnExtractors.AthleteFullName(e.Athlete));
            AddColumn("Athlete.Club", "Club", e => e.Athlete.Club);
            AddColumn("Athlete.CountryName", "Country", e => e.Athlete.CountryName);
            AddColumn("Athlete.Sex", "Sex", e => e.Athlete.Sex);
            AddColumn("Athlete.Category", "Category", e => e.Athlete.Category);
            AddColumn("Athlete.ModeratorNotes", "Notes", e => e.Athlete.ModeratorNotes);

            AddColumn("Discipline.DisciplineId", "Discipline", e => e.Discipline.DisciplineId);
            AddColumn("Discipline.Name", "Discipline", e => e.Discipline.Name);
            AddColumn("Discipline.Rules", "Rules", e => e.Discipline.Rules);

            AddColumn("Announcement.Duration", "Announced", e => ExportedTableColumnExtractors.PerformanceDuration(e.Announcement.Performance));
            AddColumn("Announcement.Depth", "Announced", e => ExportedTableColumnExtractors.PerformanceDepth(e.Announcement.Performance));
            AddColumn("Announcement.Distance", "Announced", e => ExportedTableColumnExtractors.PerformanceDistance(e.Announcement.Performance));
            AddColumn("Announcement.Points", "Announced", e => ExportedTableColumnExtractors.PerformancePoints(e.Announcement.Performance));
            AddColumn(new ExportedTableColumnPrimaryComponent<StartingListReportEntry>(
                "Announcement.Primary", "Announced", rulesRepository, e => e.Discipline.Rules, e => e.Announcement.Performance));

            AddColumn("Start.StartingLaneId", "Lane", e => e.Start.StartingLaneId);
            AddColumn("Start.StartingLaneLongName", "Lane", e => e.Start.StartingLaneLongName);
            AddColumn("Start.WarmUpTime", "WU", e => ExportedTableColumnExtractors.StartTime(e.Start.WarmUpTime));
            AddColumn("Start.OfficialTop", "OT", e => ExportedTableColumnExtractors.StartTime(e.Start.OfficialTop));

            AddColumn("CurrentResult.Actual.Duration", "Realized", e => ExportedTableColumnExtractors.PerformanceDuration(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Actual.Depth", "Realized", e => ExportedTableColumnExtractors.PerformanceDepth(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Actual.Distance", "Realized", e => ExportedTableColumnExtractors.PerformanceDistance(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Actual.Points", "Realized", e => ExportedTableColumnExtractors.PerformancePoints(e.CurrentResult.FinalPerformance));
            AddColumn(new ExportedTableColumnPrimaryComponent<StartingListReportEntry>(
                "CurrentResult.Actual.Primary", "Realized", rulesRepository, e => e.Discipline.Rules, e => e.CurrentResult.Performance));

            AddColumn("CurrentResult.Final.Duration", "Realized", e => ExportedTableColumnExtractors.PerformanceDuration(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Final.Depth", "Realized", e => ExportedTableColumnExtractors.PerformanceDepth(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Final.Distance", "Realized", e => ExportedTableColumnExtractors.PerformanceDistance(e.CurrentResult.FinalPerformance));
            AddColumn("CurrentResult.Final.Points", "Realized", e => ExportedTableColumnExtractors.PerformancePoints(e.CurrentResult.FinalPerformance));
            AddColumn(new ExportedTableColumnPrimaryComponent<StartingListReportEntry>(
                "CurrentResult.Final.Primary", "Realized", rulesRepository, e => e.Discipline.Rules, e => e.CurrentResult.Performance));

            AddColumn("CurrentResult.CardResult", "Card", e => e.CurrentResult.CardResult);
            AddColumn("CurrentResult.JudgeComment", "Comment", e => e.CurrentResult.JudgeComment);

            minimal = BuildExporter(
                "Athlete.FullName",
                "Athlete.CountryName",
                "Start.OfficialTop",
                "Start.StartingLaneLongName",
                "Announcement.Primary"
                );
            running = BuildExporter(
                "Athlete.FullName",
                "Athlete.CountryName",
                "Start.OfficialTop",
                "Start.StartingLaneLongName",
                "Announcement.Primary",
                "CurrentResult.Actual.Primary",
                "CurrentResult.CardResult",
                "CurrentResult.JudgeComment"
                );
        }

        private void AddColumn(IExportedTableColumn<StartingListReportEntry> column)
        {
            availableColumns[column.Key] = column;
        }

        private void AddColumn(string key, string title, Func<StartingListReportEntry, string> extractor)
        {
            availableColumns[key] = new ExportedTableColumnManual<StartingListReportEntry>(key, title, extractor);
        }

        private PreparedStartingListExporter BuildExporter(params string[] fieldKeys)
        {
            var columns = new List<IExportedTableColumn<StartingListReportEntry>>();
            foreach (var fieldKey in fieldKeys)
            {
                if (availableColumns.TryGetValue(fieldKey, out IExportedTableColumn<StartingListReportEntry> column))
                {
                    columns.Add(column);
                }
            }
            return new PreparedStartingListExporter(columns);
        }

        public Func<StartingListReport, ExportedTable> GetExporter(string preset)
        {
            if (preset == null) return minimal.Export;
            switch (preset)
            {
                case "minimal":
                    return minimal.Export;
                case "running":
                    return running.Export;
                default:
                    return minimal.Export;
            }
        }
    }

    public class PreparedStartingListExporter
    {
        private readonly List<IExportedTableColumn<StartingListReportEntry>> columns;

        public PreparedStartingListExporter(List<IExportedTableColumn<StartingListReportEntry>> columns)
        {
            this.columns = columns;
        }

        public ExportedTable Export(StartingListReport report)
        {
            ExportedTable table = new ExportedTable();
            table.Title = report.Title;
            foreach (var column in columns)
            {
                table.Headers.Add(column.Title);
            }
            foreach (var entry in report.Entries)
            {
                var row = new List<string>();
                foreach (var column in columns)
                {
                    row.Add(column.Extract(entry));
                }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}