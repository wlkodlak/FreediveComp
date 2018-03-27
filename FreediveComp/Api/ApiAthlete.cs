using System;

namespace FreediveComp.Api
{
    public interface IApiAthlete
    {
        Athlete GetAthlete(string raceId, string athleteId);
        void PostAthlete(string raceId, string athleteId, Athlete athlete);
        void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ReportActualResult result);
    }

    public class ApiAthlete : IApiAthlete
    {
        public Athlete GetAthlete(string raceId, string athleteId)
        {
            throw new NotImplementedException();
        }

        public void PostAthlete(string raceId, string athleteId, Athlete athlete)
        {
            throw new NotImplementedException();
        }

        public void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ReportActualResult result)
        {
            throw new NotImplementedException();
        }
    }
}