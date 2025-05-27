using System.ComponentModel;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

// User Secrets から設定を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
// AOAI にデプロイしているモデル名
var modelDeploymentName = configuration["AOAI:ModelDeploymentName"]
    ?? throw new ArgumentNullException("AOAI:ModelDeploymentName is not set in the configuration.");
// AOAI のエンドポイント
var endpoint = configuration["AOAI:Endpoint"]
    ?? throw new ArgumentNullException("AOAI:Endpoint is not set in the configuration.");

// Builder を作成
var builder = Kernel.CreateBuilder();
// Azure OpenAI 用の Chat Client を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());
// TimePlugin を登録
builder.Plugins.AddFromType<TimePlugin>();

// Kernel を作成
var kernel = builder.Build();

// Kernel のサービスから IChatClient を取得
var chatClient = kernel.Services.GetRequiredService<IChatClient>();

// IChatClient を使って関数を呼び出す
// PromptExecutionSettings から ChatOptions への変換を使って関数を自動で呼び出す
var response1 = await chatClient.GetResponseAsync("今日は何日？",
    new PromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }.ToChatOptions(kernel));

#pragma warning disable SKEXP0001
// KernelFunction を AIFunction に変換してツールとして登録
// プレビューなので SKEXP0001 を disable にしないと使えない
var response2 = await chatClient.GetResponseAsync("明日は何日？",
    new ChatOptions
    {
        ToolMode = ChatToolMode.Auto,
        Tools = [.. kernel.Plugins.SelectMany(x => x.AsAIFunctions(kernel))]
    });
#pragma warning restore SKEXP0001

Console.WriteLine($"今日は何日？→{response1.Text}");
Console.WriteLine($"明日は何日？→{response2.Text}");

// クラスでプラグインを定義
[Description("A plugin that provides time-related functions.")]
class TimePlugin
{
    [KernelFunction, Description("Get the current local time.")]
    [return: Description("The current local time as a DateTimeOffset object.")]
    public DateTimeOffset GetLocalNow() => TimeProvider.System.GetLocalNow();
}
