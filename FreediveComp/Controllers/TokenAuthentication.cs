using MilanWilczak.FreediveComp.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using System.Net.Http;

namespace MilanWilczak.FreediveComp.Controllers
{
    public class TokenAuthenticationFilter : IAuthenticationFilter
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly AuthenticationToken adminToken;

        public TokenAuthenticationFilter(IRepositorySetProvider repositorySetProvider, AuthenticationToken adminToken)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.adminToken = adminToken;
        }

        public bool AllowMultiple => true;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var task = new TaskCompletionSource<object>();
            try
            {
                Authenticate(context);
                task.SetResult(null);
            }
            catch (Exception e)
            {
                task.SetException(e);
            }
            return task.Task;
        }

        private void Authenticate(HttpAuthenticationContext context)
        {
            var headers = context.Request.Headers;
            if (!headers.Contains("X-Authentication-Token")) return;
            var fullTokenString = headers.GetValues("X-Authentication-Token").FirstOrDefault();
            var token = AuthenticationToken.Parse(fullTokenString);
            if (token == null || !token.Valid) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

            if (token.Equals(adminToken))
            {
                var judge = new Judge
                {
                    IsAdmin = true,
                    JudgeId = "admin",
                    Name = "Admin"
                };
                context.Principal = new JudgePrincipal(judge);
            }
            else
            {
                var repository = repositorySetProvider.GetRepositorySet(token.RaceId).Judges;

                var judge = repository.FindJudge(token.JudgeId);
                if (judge == null) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

                var tokenVerified = repository.FindJudgesDevices(token.JudgeId).Any(d => d.AuthenticationToken == fullTokenString);
                if (!tokenVerified) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

                context.Principal = new JudgePrincipal(judge);
            }
        }

        private void BuildAdminJudge()
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    public class IpAuthenticationFilter : IAuthenticationFilter
    {
        private readonly bool disabledByAdminToken;

        public IpAuthenticationFilter(AuthenticationToken adminToken)
        {
            disabledByAdminToken = adminToken != null && adminToken.Valid;
        }

        public bool AllowMultiple => true;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var task = new TaskCompletionSource<object>();
            try
            {
                Authenticate(context);
                task.SetResult(null);
            }
            catch (Exception e)
            {
                task.SetException(e);
            }
            return task.Task;
        }

        private void Authenticate(HttpAuthenticationContext context)
        {
            if (disabledByAdminToken) return;

            var ip = context.Request.GetOwinContext().Request.RemoteIpAddress;
            var local = ip == "127.0.0.1" || ip == "::1";
            if (!local) return;

            var judge = new Judge
            {
                IsAdmin = true,
                JudgeId = "admin",
                Name = "Admin"
            };
            context.Principal = new JudgePrincipal(judge);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}