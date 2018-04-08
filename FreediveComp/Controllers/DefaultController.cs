using FreediveComp.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FreediveComp.Controllers
{
    [DefaultExceptionFilter]
    public class DefaultController : ApiController
    {
        private readonly IApiSearch apiSearch;
        private readonly IApiRules apiRules;
        private readonly IApiSetup apiSetup;
        private readonly IApiAuthentication apiAuthentication;
        private readonly IApiAthlete apiAthlete;
        private readonly IApiReports apiReports;
        private readonly IApiStartingList apiStartingList;

        [Route("api-1.0/global/search")]
        public List<RaceSearchResultDto> GetGlobalSearch(string query, DateTimeOffset? date)
        {
            return apiSearch.GetSearch(query, date);
        }

        [Route("api-1.0/global/rules")]
        public List<RulesDto> GetGlobalRules()
        {
            return apiRules.GetRules();
        }

        [Route("api-1.0/global/rules/{rulesName}/points")]
        public double PostGlobalRulesPoints(string rulesName, PerformanceDto performance)
        {
            return apiRules.GetPoints(rulesName, performance);
        }

        [Route("api-1.0/global/rules/{rulesName}/short")]
        public PenalizationDto PostGlobalRulesShort(string rulesName, GetShortPenalizationRequest request)
        {
            return apiRules.GetShort(rulesName, request);
        }

        [Route("api-1.0/global/rules/{rulesName}/penalization")]
        public PenalizationDto PostGlobalRulesPenalization(string rulesName, GetCalculatedPenalizationRequest request)
        {
            return apiRules.GetPenalization(rulesName, request);
        }

        [Route("api-1.0/{raceId}/setup")]
        public RaceSetupDto GetRaceSetup(string raceId)
        {
            return apiSetup.GetSetup(raceId);
        }

        [Route("api-1.0/{raceId}/setup")]
        public void PostRaceSetup(string raceId, RaceSetupDto raceSetup)
        {
            apiSetup.SetupRace(raceId, raceSetup);
        }

        [Route("api-1.0/{raceId}/auth/authorize")]
        public JudgeDto PostAuthAuthorize(string raceId, AuthorizeRequestDto authorization)
        {
            return apiAuthentication.Authorize(raceId, authorization);
        }

        [Route("api-1.0/{raceId}/auth/authenticate")]
        public AuthenticateResponseDto PostAuthAuthenticate(string raceId, AuthenticateRequestDto authentication)
        {
            return apiAuthentication.Authenticate(raceId, authentication);
        }

        [Route("api-1.0/{raceId}/auth/judges")]
        public List<JudgeDto> GetAuthJudges(string raceId)
        {
            return apiAuthentication.GetJudges(raceId);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}")]
        public AthleteDto GetAthlete(string raceId, string athleteId)
        {
            return apiAthlete.GetAthlete(raceId, athleteId);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}")]
        public void PostAthlete(string raceId, string athleteId, AthleteDto athlete)
        {
            apiAthlete.PostAthlete(raceId, athleteId, athlete);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}/results")]
        public void PostAthleteResult(string raceId, string athleteId, [FromHeader("X-Authentication-Token")] string authenticationToken, ActualResultDto result)
        {
            apiAthlete.PostAthleteResult(raceId, athleteId, authenticationToken, result);
        }

        [Route("api-1.0/{raceId}/reports/start/{startingLaneId?}")]
        public StartingListReport GetReportStartingList(string raceId, string startingLaneId)
        {
            return apiReports.GetReportStartingList(raceId, startingLaneId);
        }

        [Route("api-1.0/{raceId}/reports/discipline/{disciplineId}")]
        public ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId)
        {
            return apiReports.GetReportDisciplineResults(raceId, disciplineId);
        }

        [Route("api-1.0/{raceId}/reports/results/{resultsListId}")]
        public ResultsListReport GetReportResultsList(string raceId, string resultsListId)
        {
            return apiReports.GetReportResultsList(raceId, resultsListId);
        }

        [Route("api-1.0/{raceId}/start/{startingLaneId?}")]
        public List<StartingListEntryDto> GetStartingList(string raceId, string startingLaneId)
        {
            return apiStartingList.GetStartingList(raceId, startingLaneId);
        }

        [Route("api-1.0/{raceId}/start/{startingLaneId?}")]
        public void PostStartingList(string raceId, string startingLaneId, List<StartingListEntryDto> startingList)
        {
            apiStartingList.SetupStartingList(raceId, startingLaneId, startingList);
        }
    }
}
