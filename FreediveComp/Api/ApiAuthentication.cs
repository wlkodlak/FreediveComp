using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiAuthentication
    {
        Judge Authorize(string raceId, AuthorizeRequest authorization);
        AuthenticateResponse Authenticate(string raceId, AuthenticateRequest authentication);
        List<Judge> GetJudges(string raceId);
    }

    public class ApiAuthentication : IApiAuthentication
    {
        public AuthenticateResponse Authenticate(string raceId, AuthenticateRequest authentication)
        {
            throw new NotImplementedException();
        }

        public Judge Authorize(string raceId, AuthorizeRequest authorization)
        {
            throw new NotImplementedException();
        }

        public List<Judge> GetJudges(string raceId)
        {
            throw new NotImplementedException();
        }
    }
}