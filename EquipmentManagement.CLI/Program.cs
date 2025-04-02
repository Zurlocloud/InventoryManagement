using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace EquipmentManagement.CLI
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string baseUrl = "http://localhost:5049/api/equipment";
        private static readonly string tenantId = "1d37c04b-c0e1-4f5c-8f3f-8e582fe7ccca";

        static async Task Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("Equipment Copilot CLI - Interactive Chat Session");
            Console.WriteLine("=================================================");
            Console.WriteLine("Type 'exit' to quit or 'clear' to reset the chat.");
            Console.WriteLine();

            // Set up HttpClient
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Main chat loop
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("You: ");
                Console.ResetColor();

                var userInput = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.ToLower() == "exit")
                    break;

                if (userInput.ToLower() == "clear")
                {
                    await ClearChatHistory();
                    continue;
                }

                try
                {
                    var response = await SendChatMessage(userInput);
                    
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("Copilot: ");
                    Console.ResetColor();
                    Console.WriteLine(response.Message);
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        static async Task<ChatResponse> SendChatMessage(string message)
        {
            var chatRequest = new ChatRequest
            {
                Message = message,
                TenantId = tenantId
            };

            var json = JsonSerializer.Serialize(chatRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{baseUrl}/chat", content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ChatResponse>();
        }

        static async Task ClearChatHistory()
        {
            try
            {
                var response = await client.PostAsync($"{baseUrl}/clear", new StringContent("{}", Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                Console.WriteLine("Chat history cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error clearing chat history: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}