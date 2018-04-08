using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace FreediveComp.Controllers
{
    public class DefaultExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            if (context.Exception is ArgumentNullException missing)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                context.Response.Content = new StringContent(missing.Message);
            }
            else if (context.Exception is ArgumentOutOfRangeException wrong)
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.BadRequest);
                context.Response.Content = new StringContent(wrong.Message);
            }
        }
    }
}