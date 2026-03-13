using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;


namespace Psychological_Support_Chatbot.Controllers
{
    public class ChatController : ApiController
    {
        [HttpPost]
        [Route("api/chat")]
        public IHttpActionResult SendMessage(ChatRequest request)
        {
            var reply = "Hello, I'm here to listen. Tell me how you feel.";

            return Ok(new { reply = reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}
