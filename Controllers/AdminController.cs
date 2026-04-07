using LiteDB;
using System;
using System.Configuration;
using System.IO;
using System.Web.Http;

namespace Psychological_Support_Chatbot.Controllers
{
    [RoutePrefix("api/admin")]
    public class AdminController : ApiController
    {
        private static readonly string dbPath = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, "App_Data", "chat_history_lite.db");

        // Use static variable for a simple session token (clears on app restart)
        public static string CurrentToken = null;

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            var adminUser = ConfigurationManager.AppSettings["AdminUsername"];
            var adminPass = ConfigurationManager.AppSettings["AdminPassword"];

            if (request != null && request.username == adminUser && request.password == adminPass)
            {
                CurrentToken = Guid.NewGuid().ToString(); // Generate session token
                return Ok(new { token = CurrentToken });
            }
            return Unauthorized();
        }

        [HttpGet, Route("prompt"), AdminAuth]
        public IHttpActionResult GetPrompt()
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<SystemConfig>("settings");
                var setting = col.FindOne(x => x.Key == "SystemPrompt");
                return Ok(new { prompt = setting?.Value ?? "You are an empathetic psychological support AI." });
            }
        }

        [HttpPost, Route("prompt"), AdminAuth]
        public IHttpActionResult UpdatePrompt([FromBody] PromptUpdateRequest request)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<SystemConfig>("settings");
                var setting = col.FindOne(x => x.Key == "SystemPrompt") ?? new SystemConfig { Key = "SystemPrompt" };
                setting.Value = request.prompt;
                col.Upsert(setting);
            }
            return Ok(new { success = true });
        }
    }
}