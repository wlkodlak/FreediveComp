using System;
using System.Security.Principal;
using System.Text;

namespace MilanWilczak.FreediveComp.Models
{
    public class AuthenticationToken
    {
        private readonly string raceId, judgeId, token;

        private AuthenticationToken(string raceId, string judgeId, string token)
        {
            this.raceId = raceId;
            this.judgeId = judgeId;
            this.token = token;
        }

        public string RaceId => raceId;
        public string JudgeId => judgeId;
        public string Token => token;

        public static AuthenticationToken Build(string raceId, string judgeId, string token)
        {
            return new AuthenticationToken(raceId, judgeId, token);
        }

        public static AuthenticationToken Generate(string raceId, string judgeId)
        {
            return new AuthenticationToken(raceId, judgeId, Guid.NewGuid().ToString("N"));
        }

        public static AuthenticationToken Parse(string fullToken)
        {
            if (string.IsNullOrEmpty(fullToken)) return null;
            var parts = fullToken.Split(':');
            if (parts.Length != 3) return null;
            return new AuthenticationToken(parts[0], parts[1], parts[2]);
        }

        public override string ToString()
        {
            return new StringBuilder().Append(raceId).Append(":").Append(judgeId).Append(":").Append(token).ToString();
        }

        public override int GetHashCode()
        {
            return token.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is AuthenticationToken oth)
            {
                return
                    raceId == oth.raceId &&
                    judgeId == oth.judgeId &&
                    token == oth.token;
            }
            else
            {
                return false;
            }
        }
    }

    public class JudgePrincipal : IPrincipal, IIdentity
    {
        private readonly Judge judge;

        public JudgePrincipal(Judge judge)
        {
            this.judge = judge;
        }

        public Judge Judge => judge;

        public IIdentity Identity => this;

        public string Name => judge.Name;

        public string AuthenticationType => "AuthenticationToken";

        public bool IsAuthenticated => true;

        public bool IsInRole(string role)
        {
            switch (role)
            {
                case "Admin": return judge.IsAdmin;
                case "Judge": return true;
                default: return false;
            }
        }
    }
}