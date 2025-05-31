using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Moq;
using OpenAI.Chat;

// 使用するフィルターを変更しながら動作確認
Console.WriteLine("=== LoggingPromptFilter ===");
await RunAsync(new (), new LoggingPromptFilter());

Console.WriteLine("=== ReplacePromptFilter ===");
await RunAsync(new(), new ReplacePromptFilter());

Console.WriteLine("=== CancelPromptFilter ===");
await RunAsync(new(), new CancelPromptFilter());

Console.WriteLine("=== CancelPromptFilter with cancel param ===");
await RunAsync(new() { ["cancel"] = "" }, new CancelPromptFilter());

Console.WriteLine("=== LoggingPromptFilter, CancelPromptFilter, ReplacePromptFilter ===");
await RunAsync(new(), new LoggingPromptFilter(), new CancelPromptFilter(), new ReplacePromptFilter());

Console.WriteLine("=== LoggingPromptFilter, CancelPromptFilter, ReplacePromptFilter with cancel param ===");
await RunAsync(new() { ["cancel"] = "" }, new LoggingPromptFilter(), new CancelPromptFilter(), new ReplacePromptFilter());

async Task RunAsync(KernelArguments arguments, params IEnumerable<IPromptRenderFilter> filters)
{
    // Kernel を作成
    var builder = Kernel.CreateBuilder();
    foreach (var filter in filters)
    {
        builder.Services.AddSingleton(filter);
    }
    // EchoChatClient を登録
    builder.AddAzureOpenAIChatClient("dummy", new MockAOAIClient());
    var kernel = builder.Build();
    // プロンプトから関数を作成
    var function = kernel.CreateFunctionFromPrompt(
        promptConfig: new("オリジナルプロンプト")
        {
            Name = "Foo",
            InputVariables = [new() { Name = "cancel" }],
        });
    // 関数を実行
    var result = await kernel.InvokeAsync(function, arguments);
    // 結果を表示
    Console.WriteLine($"結果: {result.GetValue<string>()}");
    Console.WriteLine();
}


// プロンプトのログをとるフィルター
class LoggingPromptFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        Console.WriteLine($"開始: {nameof(LoggingPromptFilter)}#{nameof(OnPromptRenderAsync)}({context.Function.Name})");
        await next(context);
        Console.WriteLine($"終了: {nameof(LoggingPromptFilter)}#{nameof(OnPromptRenderAsync)}({context.Function.Name}) => {context.RenderedPrompt}");
    }
}

// 引数に cancel がるとキャンセルするフィルター
class CancelPromptFilter : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        if (context.Arguments.ContainsKey("cancel"))
        {
            context.Result = new(context.Function, "キャンセルされました");
            return;
        }

        await next(context);
    }
}

// プロンプトを置き換えるフィルター
class ReplacePromptFilter : IPromptRenderFilter
{
    public Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        context.RenderedPrompt = "フィルターで置き換えられたプロンプト";
        return Task.CompletedTask;
    }
}


// 渡されたテキストに対して「Echo: テキスト」と応答するモッククライアント
class MockAOAIClient : AzureOpenAIClient
{
    public override ChatClient GetChatClient(string deploymentName) =>
        new MockChatClient();

    class MockChatClient : ChatClient
    {
        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(
            IEnumerable<ChatMessage> messages, 
            ChatCompletionOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            var text = messages.Last().Content[0].Text;
            var clientResult = ClientResult.FromValue(
                OpenAIChatModelFactory.ChatCompletion(
                    role: ChatMessageRole.Assistant,
                    content: new ($"Echo: {text}")),
                Mock.Of<PipelineResponse>());
            return Task.FromResult(clientResult);
        }
    }
}
