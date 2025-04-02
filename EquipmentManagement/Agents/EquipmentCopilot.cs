using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using EquipmentManagement.Services;
using EquipmentManagement.Models;
using System.Text.Json;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;

namespace EquipmentManagement.Agents;

public class EquipmentCopilot(Kernel kernel, ILogger<EquipmentCopilot> logger)
{
    private readonly Kernel _kernel = kernel;

    private readonly ILogger<EquipmentCopilot> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ChatHistory _chatHistory = new("""
    You are EquipmentCopilot, a friendly assistant who likes to follow the rules. You will complete required steps
     and request approval before taking any consequential actions, such as saving the request to the database.
     If the user doesn't provide enough information for you to complete a task, you will keep asking questions
     until you have enough information to complete the task. Once the request has been saved to the database,
     inform the user that the equipment has been added to the inventory database.
    """);

    public async Task<string> ProcessMessageAsync(string message, string tenantId)
    {
        try
        {
            // Get chat completion service from kernel
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            // Configure chat completion settings with function calling enabled
            var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings()
            {
               ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Add user message to chat history
            _chatHistory.AddUserMessage(message);
            
            _logger.LogInformation("updated chat history: {history}", _chatHistory);

            // Process the message and generate response
            var response = await chatCompletionService.GetChatMessageContentAsync(
                _chatHistory, 
                executionSettings: openAIPromptExecutionSettings,
                _kernel
            );

            // Add assistant message to chat history
            _chatHistory.AddAssistantMessage(response.Content ?? "I wasn't able to process your request.");
                        
            return response.Content ?? "I wasn't able to process your request.";
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error processing message: {Message}", message);
            return $"I encountered an error while processing your request: {ex.Message}";
        }
    }
}