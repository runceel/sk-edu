using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var services = new ServiceCollection();
services.AddSingleton<IChatClient>(new EchoChatClient("default"));
services.AddKeyedSingleton<IChatClient>("key1", new EchoChatClient("key1"));
services.AddKeyedSingleton<IChatClient>("key2", new EchoChatClient("key2"));

services.AddKernel();

var provider = services.BuildServiceProvider();

var kernel = provider.GetRequiredService<Kernel>();
var promptFunction = kernel.CreateFunctionFromPrompt("Hello");

// デフォルトの IChatClient が使われる
var result = await kernel.InvokeAsync(promptFunction);
Console.WriteLine(result.GetValue<string>());

// キーを指定して使用する IChatClient を指定する
var result1 = await kernel.InvokeAsync(promptFunction,
    arguments: new(new PromptExecutionSettings { ServiceId = "key1" }));
Console.WriteLine(result1.GetValue<string>());
var result2 = await kernel.InvokeAsync(promptFunction,
    arguments: new(new PromptExecutionSettings { ServiceId = "key2" }));
Console.WriteLine(result2.GetValue<string>());

// ダミーの IChatClient 実装
class EchoChatClient(string name) : IChatClient
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.LastOrDefault()?.Text ?? "";
        return Task.FromResult(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Echo: {lastMessage} by {name}")));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(ChatClientMetadata))
        {
            return new ChatClientMetadata("mock");
        }

        return null;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
