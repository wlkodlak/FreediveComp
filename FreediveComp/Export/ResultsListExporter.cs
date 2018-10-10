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
        public ResultsListExporter(IRulesRepository rulesRepository)
        {

        }

        public Func<ResultsListReport, ExportedTable> GetExporter(string preset)
        {
            throw new NotImplementedException();
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
            athleteFields.Add(field);
        }

        public void AddResultField(IExportedTableColumn<ResultsListReportEntrySubresult> field, bool hasPerformance, bool hasPoints)
        {
            var fieldSet = GetFieldSet(hasPerformance, hasPoints);
            if (fieldSet != null) fieldSet.Add(field);
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