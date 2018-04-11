using MilanWilczak.FreediveComp.Api;
using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web.Http;

namespace MilanWilczak.FreediveComp.Controllers
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

        public DefaultController(
            IApiSearch apiSearch, IApiRules apiRules, IApiSetup apiSetup, IApiAuthentication apiAuthentication,
            IApiAthlete apiAthlete, IApiReports apiReports, IApiStartingList apiStartingList)
        {
            this.apiSearch = apiSearch;
            this.apiRules = apiRules;
            this.apiSetup = apiSetup;
            this.apiAuthentication = apiAuthentication;
            this.apiAthlete = apiAthlete;
            this.apiReports = apiReports;
            this.apiStartingList = apiStartingList;
        }

        [Route("api-1.0/global/search")]
        [AllowAnonymous]
        public List<RaceSearchResultDto> GetGlobalSearch(string query, DateTimeOffset? date)
        {
            return apiSearch.GetSearch(query, date);
        }

        [Route("api-1.0/global/rules")]
        [AllowAnonymous]
        public List<RulesDto> GetGlobalRules()
        {
            return apiRules.GetRules();
        }

        [Route("api-1.0/global/rules/{rulesName}/points")]
        [AllowAnonymous]
        public double PostGlobalRulesPoints(string rulesName, PerformanceDto performance)
        {
            return apiRules.GetPoints(rulesName, performance);
        }

        [Route("api-1.0/global/rules/{rulesName}/short")]
        [AllowAnonymous]
        public PenalizationDto PostGlobalRulesShort(string rulesName, GetShortPenalizationRequest request)
        {
            return apiRules.GetShort(rulesName, request);
        }

        [Route("api-1.0/global/rules/{rulesName}/penalization")]
        [AllowAnonymous]
        public PenalizationDto PostGlobalRulesPenalization(string rulesName, GetCalculatedPenalizationRequest request)
        {
            return apiRules.GetPenalization(rulesName, request);
        }

        [Route("api-1.0/{raceId}/setup")]
        [AllowAnonymous]
        public RaceSetupDto GetRaceSetup(string raceId)
        {
            return apiSetup.GetSetup(raceId);
        }

        [Route("api-1.0/{raceId}/setup")]
        [Authorize(Roles = "Admin")]
        public void PostRaceSetup(string raceId, RaceSetupDto raceSetup)
        {
            apiSetup.SetupRace(raceId, raceSetup);
        }

        [Route("api-1.0/{raceId}/auth/authorize")]
        [Authorize(Roles = "Admin")]
        public JudgeDto PostAuthAuthorize(string raceId, AuthorizeRequestDto authorization)
        {
            return apiAuthentication.Authorize(raceId, authorization);
        }

        [Route("api-1.0/{raceId}/auth/authenticate")]
        [AllowAnonymous]
        public AuthenticateResponseDto PostAuthAuthenticate(string raceId, AuthenticateRequestDto authentication)
        {
            return apiAuthentication.Authenticate(raceId, authentication);
        }

        [Route("api-1.0/{raceId}/auth/judges")]
        [AllowAnonymous]
        public List<JudgeDto> GetAuthJudges(string raceId)
        {
            return apiAuthentication.GetJudges(raceId);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}")]
        [AllowAnonymous]
        public AthleteDto GetAthlete(string raceId, string athleteId)
        {
            return apiAthlete.GetAthlete(raceId, athleteId);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}")]
        [Authorize(Roles = "Admin")]
        public void PostAthlete(string raceId, string athleteId, AthleteDto athlete)
        {
            apiAthlete.PostAthlete(raceId, athleteId, athlete);
        }

        [Route("api-1.0/{raceId}/athletes/{athleteId}/results")]
        [Authorize(Roles = "Judge")]
        public void PostAthleteResult(string raceId, string athleteId, IPrincipal principal, ActualResultDto result)
        {
            var judgePrincipal = (JudgePrincipal)principal;
            apiAthlete.PostAthleteResult(raceId, athleteId, judgePrincipal.Judge, result);
        }

        [Route("api-1.0/{raceId}/reports/start/{startingLaneId?}")]
        [AllowAnonymous]
        public StartingListReport GetReportStartingList(string raceId, string startingLaneId)
        {
            return apiReports.GetReportStartingList(raceId, startingLaneId);
        }

        [Route("api-1.0/{raceId}/reports/discipline/{disciplineId}")]
        [AllowAnonymous]
        public ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId)
        {
            return apiReports.GetReportDisciplineResults(raceId, disciplineId);
        }

        [Route("api-1.0/{raceId}/reports/results/{resultsListId}")]
        [AllowAnonymous]
        public ResultsListReport GetReportResultsList(string raceId, string resultsListId)
        {
            return apiReports.GetReportResultsList(raceId, resultsListId);
        }

        [Route("api-1.0/{raceId}/start/{startingLaneId?}")]
        [AllowAnonymous]
        public List<StartingListEntryDto> GetStartingList(string raceId, string startingLaneId)
        {
            return apiStartingList.GetStartingList(raceId, startingLaneId);
        }

        [Route("api-1.0/{raceId}/start/{startingLaneId?}")]
        [Authorize(Roles = "Admin")]
        public void PostStartingList(string raceId, string startingLaneId, List<StartingListEntryDto> startingList)
        {
            apiStartingList.SetupStartingList(raceId, startingLaneId, startingList);
        }
    }
}
