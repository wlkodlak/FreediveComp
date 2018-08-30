using MilanWilczak.FreediveComp.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Web.Http.Filters;
using System.Web.Http.Results;

namespace MilanWilczak.FreediveComp.Controllers
{
    public class TokenAuthenticationFilter : IAuthenticationFilter
    {
        private readonly IRepositorySetProvider repositorySetProvider;

        public TokenAuthenticationFilter(IRepositorySetProvider repositorySetProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
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
            if (fullTokenString == null) return;

            var token = AuthenticationToken.Parse(fullTokenString);
            if (token == null) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

            var repository = repositorySetProvider.GetRepositorySet(token.RaceId).Judges;

            var judge = repository.FindJudge(token.JudgeId);
            if (judge == null) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

            var tokenVerified = repository.FindJudgesDevices(token.JudgeId).Any(d => d.AuthenticationToken == fullTokenString);
            if (!tokenVerified) context.ErrorResult = new UnauthorizedResult(new AuthenticationHeaderValue[0], context.Request);

            context.Principal = new JudgePrincipal(judge);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}