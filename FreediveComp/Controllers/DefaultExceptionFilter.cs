using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace MilanWilczak.FreediveComp.Controllers
{
    public class DefaultExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            context.Response = context.ActionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, context.Exception.Message);
        }
    }
}