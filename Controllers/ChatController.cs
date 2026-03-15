using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Psychological_Support_Chatbot.Controllers
{
    public class ChatController : ApiController
    {
        private static readonly string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"]; 
        private static readonly string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";


        private static readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat_history.db");
        private static readonly string connectionString = $"Data Source={dbPath};Version=3;";

        public ChatController()
        {
            InitializeDatabase();
        }

        [HttpPost]
        [Route("api/chat")]
        public async Task<IHttpActionResult> SendMessage(ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("SessionId is required.");
            }


            SaveMessageToDb(request.SessionId, "user", request.Message);


            var emotion = await DetectEmotion(request.Message);


            var history = GetHistoryFromDb(request.SessionId);


            var reply = await GetAIResponse(history);


            SaveMessageToDb(request.SessionId, "model", reply);

            return Ok(new
            {
                reply = reply,
                emotion = emotion
            });
        }

        private async Task<string> GetAIResponse(List<MessageHistory> history)
        {
            using (var client = new HttpClient())
            {
                var contents = new List<object>();


                foreach (var item in history)
                {
                    contents.Add(new
                    {
                        role = item.role,
                        parts = item.parts
                    });
                }

                var body = new
                {
                    contents = contents.ToArray(),
                    system_instruction = new
                    {
                        parts = new[] { new { text = "You are a supportive AI. You MUST use the conversation history provided to remember the user's name and previous feelings." } }
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync($"{geminiUrl}?key={apiKey}",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"AI Error ({response.StatusCode}): {result}";
                }

                dynamic data = JsonConvert.DeserializeObject(result);
                return data.candidates[0].content.parts[0].text.ToString();
            }
        }

        // --- SQLite Database ---

        private void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Messages (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            SessionId TEXT NOT NULL,
                            Role TEXT NOT NULL,
                            Text TEXT NOT NULL,
                            Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";
                    using (var command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SaveMessageToDb(string sessionId, string role, string text)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string insertQuery = "INSERT INTO Messages (SessionId, Role, Text) VALUES (@SessionId, @Role, @Text)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@SessionId", sessionId);
                    command.Parameters.AddWithValue("@Role", role);
                    command.Parameters.AddWithValue("@Text", text);
                    command.ExecuteNonQuery();
                }
            }
        }

        private List<MessageHistory> GetHistoryFromDb(string sessionId)
        {
            var history = new List<MessageHistory>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT Role, Text FROM (SELECT Role, Text, Timestamp FROM Messages WHERE SessionId = @SessionId ORDER BY Timestamp DESC LIMIT 30) ORDER BY Timestamp ASC";

                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@SessionId", sessionId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            history.Add(new MessageHistory
                            {
                                role = reader["Role"].ToString(),
                                parts = new[] { new Part { text = reader["Text"].ToString() } }
                            });
                        }
                    }
                }
            }
            return history;
        }


        private async Task<string> DetectEmotion(string message)
        {
            using (var client = new HttpClient())
            {
                var body = new
                {
                    contents = new[] { new { parts = new[] { new { text = $"Classify the emotional tone of this message as Positive, Neutral, or Negative. Only return the word. Message: {message}" } } } }
                };
                var json = JsonConvert.SerializeObject(body);
                var response = await client.PostAsync($"{geminiUrl}?key={apiKey}",
                    new StringContent(json, Encoding.UTF8, "application/json"));
                var result = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return "Error";
                dynamic data = JsonConvert.DeserializeObject(result);
                return data.candidates[0].content.parts[0].text.ToString().Trim();
            }
        }
    }


    public class MessageHistory
    {
        public string role { get; set; }
        public Part[] parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }

    public class ChatRequest
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; } 
    }
}