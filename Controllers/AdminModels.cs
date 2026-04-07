using System;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Psychological_Support_Chatbot.Controllers
{
    // storing configuration in LiteDB
    public class SystemConfig
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    // API Requests
    public class LoginRequest { public string username { get; set; } public string password { get; set; } }
    public class PromptUpdateRequest { public string prompt { get; set; } }

    // Auth
    public class AdminAuthAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var authHeader = actionContext.Request.Headers.Authorization;

            // Check token 
            if (authHeader == null || authHeader.Scheme != "Bearer" || authHeader.Parameter != AdminController.CurrentToken)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "Unauthorized");
            }
        }
    }
}