using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiReports
    {
        List<StartingLaneReportEntry> GetReportStartingList(string raceId, string startingLaneId);
        ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId);
        ResultsListReport GetReportResultsList(string raceId, string resultsListId);
    }

    public class ApiReports : IApiReports
    {
        public ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId)
        {
            throw new NotImplementedException();
        }

        public ResultsListReport GetReportResultsList(string raceId, string resultsListId)
        {
            throw new NotImplementedException();
        }

        public List<StartingLaneReportEntry> GetReportStartingList(string raceId, string startingLaneId)
        {
            throw new NotImplementedException();
        }
    }
}