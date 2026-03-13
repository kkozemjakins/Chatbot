using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using System.Configuration;

namespace Psychological_Support_Chatbot.Controllers
{
    public class ChatController : ApiController
    {
        // Using the key from your Web.config
        private static readonly string apiKey = ConfigurationManager.AppSettings["OpenAIApiKey"];
        // FIXED: Changed '/' to ':' before generateContent and upgraded to 2.0-flash
        private static readonly string geminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        [HttpPost]
        [Route("api/chat")]
        public async Task<IHttpActionResult> SendMessage(ChatRequest request)
        {
            // Gemini is fast enough to run these in parallel if you prefer, 
            // but keeping your original sequential flow for simplicity.
            var emotion = await DetectEmotion(request.Message);
            var reply = await GetAIResponse(request.Message);

            return Ok(new
            {
                reply = reply,
                emotion = emotion
            });
        }

        [HttpGet]
        [Route("api/chat/models")]
        public async Task<IHttpActionResult> ListModels()
        {
            using (var client = new HttpClient())
            {
                // GET request to the models endpoint
                var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}");
                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return BadRequest(result);

                return Ok(JsonConvert.DeserializeObject(result));
            }
        }

        private async Task<string> GetAIResponse(string message)
        {
            using (var client = new HttpClient())
            {
                var body = new
                {
                    // Note: system_instruction.parts should be an array
                    system_instruction = new { parts = new[] { new { text = "You are a supportive psychological chatbot. Be empathetic and encouraging." } } },
                    contents = new[]
                    {
                new { parts = new[] { new { text = message } } }
            }
                };

                var json = JsonConvert.SerializeObject(body);

                // Append the API key as a query parameter
                var response = await client.PostAsync($"{geminiUrl}?key={apiKey}",
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // This will help you see if there are still permission or region issues
                    return $"AI Error ({response.StatusCode}): {result}";
                }

                dynamic data = JsonConvert.DeserializeObject(result);
                return data.candidates[0].content.parts[0].text.ToString();
            }
        }

        private async Task<string> DetectEmotion(string message)
        {
            using (var client = new HttpClient())
            {
                var body = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = $"Classify the emotional tone of this message as Positive, Neutral, or Negative. Only return the word. Message: {message}" } } }
                    }
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

    public class ChatRequest
    {
        public string Message { get; set; }
    }
}