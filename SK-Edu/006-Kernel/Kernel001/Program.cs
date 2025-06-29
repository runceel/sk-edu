﻿using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using Moq;
using OpenAI.Chat;

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatClient(
    "model1",
    new DummyOpenAIClient("service1"),
    serviceId: "service1",
    modelId: "model1");
builder.AddAzureOpenAIChatClient(
    "model2",
    new DummyOpenAIClient("service2"),
    serviceId: "service2",
    modelId: "model2");

var kernel = builder.Build();

var func1 = kernel.CreateFunctionFromPrompt("Test prompt 1",
    new PromptExecutionSettings
    {
        ServiceId = "service1",
        ModelId = "model1",
    });
var func2 = kernel.CreateFunctionFromPrompt("Test prompt 2",
    new PromptExecutionSettings
    {
        ServiceId = "service2",
        ModelId = "model2",
    });

var result1 = await func1.InvokeAsync(kernel);
var result2 = await func2.InvokeAsync(kernel);

Console.WriteLine($"result1: {result1.GetValue<string>()}");
Console.WriteLine($"result2: {result2.GetValue<string>()}");


class DummyOpenAIClient(string id) : AzureOpenAIClient
{
    public override ChatClient GetChatClient(string deploymentName) => 
        new DummyChatClient($"{id}-{deploymentName}");

    class DummyChatClient(string id) : ChatClient
    {
        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(ClientResult.FromValue(
                OpenAIChatModelFactory.ChatCompletion(
                    role: ChatMessageRole.Assistant,
                    content: new OpenAI.Chat.ChatMessageContent(
                        $"reply message: {messages.Last().Content[0].Text} generated by {id}.")),
                Mock.Of<PipelineResponse>()));

        public override AsyncCollectionResult<StreamingChatCompletionUpdate> CompleteChatStreamingAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options = null, CancellationToken cancellationToken = default)
        {
            return base.CompleteChatStreamingAsync(messages, options, cancellationToken);
        }
    }
}
