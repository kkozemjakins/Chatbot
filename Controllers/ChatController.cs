using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using LiteDB; 

namespace Psychological_Support_Chatbot.Controllers
{
    public class ChatController : ApiController
    {
        private static readonly string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
        private static readonly string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";


        private static readonly string dbPath = Path.Combine(System.Web.HttpRuntime.AppDomainAppPath, "App_Data", "chat_history_lite.db");

        [HttpPost]
        [Route("api/chat")]
        public async Task<IHttpActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.sessionId))
                return BadRequest("SessionId is required.");

            // Saves User Message
            SaveMessageToDb(request.sessionId, "user", request.message);

            // Processes AI logic
            var emotion = await DetectEmotion(request.message);
            var history = GetHistoryFromDb(request.sessionId);
            var reply = await GetAIResponse(history);

            // Saves Bot Message
            SaveMessageToDb(request.sessionId, "model", reply);

            return Ok(new { reply = reply, emotion = emotion });
        }

        private void SaveMessageToDb(string sessionId, string role, string text)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<MessageRecord>("messages");
                col.Insert(new MessageRecord
                {
                    SessionId = sessionId,
                    Role = role,
                    Text = text,
                    Timestamp = DateTime.Now
                });
            }
        }

        private List<MessageHistory> GetHistoryFromDb(string sessionId)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                var col = db.GetCollection<MessageRecord>("messages");

                // Last 30 messages, ordered by time
                var results = col.Find(x => x.SessionId == sessionId)
                                 .OrderByDescending(x => x.Timestamp)
                                 .Take(30)
                                 .Reverse()
                                 .ToList();

                return results.Select(r => new MessageHistory
                {
                    role = r.Role,
                    parts = new[] { new Part { text = r.Text } }
                }).ToList();
            }
        }



        private async Task<string> GetAIResponse(List<MessageHistory> history)
        {
            using (var client = new HttpClient())
            {
                var contents = history.Select(h => new { role = h.role, parts = h.parts }).ToList();
                var body = new
                {
                    contents = contents.ToArray(),
                    system_instruction = new { parts = new[] { new { text = "You are an empathetic psychological support AI. Respond kindly and supportively." } } }
                };

                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync($"{geminiUrl}?key={apiKey}", new StringContent(json, Encoding.UTF8, "application/json"));
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return $"AI Error: {response.StatusCode}";

                dynamic data = JsonConvert.DeserializeObject(result);
                return data.candidates[0].content.parts[0].text.ToString();
            }
        }

        private async Task<string> DetectEmotion(string message)
        {
            using (var client = new HttpClient())
            {
                var body = new { contents = new[] { new { parts = new[] { new { text = $"Classify the emotion as Positive, Neutral, or Negative: {message}" } } } } };
                var response = await client.PostAsync($"{geminiUrl}?key={apiKey}", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode) return "Neutral";
                dynamic data = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                return data.candidates[0].content.parts[0].text.ToString().Trim();
            }
        }
    }


    public class MessageRecord
    {
        public int Id { get; set; }
        public string SessionId { get; set; }
        public string Role { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
    }


    public class MessageHistory { public string role { get; set; } public Part[] parts { get; set; } }
    public class Part { public string text { get; set; } }
    public class ChatRequest { public string message { get; set; } public string sessionId { get; set; } }
}