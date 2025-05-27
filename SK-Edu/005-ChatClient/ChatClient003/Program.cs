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

// AsKernelFunction はまだプレビュー機能なので SKEXP0001 を disable にしないと使えない
#pragma warning disable SKEXP0001
// AITool (AIFunction) を KernelFunction に変換してプラグインとして登録
builder.Plugins.AddFromFunctions("TimePlugin",
    [AIFunctionFactory.Create(TimeTools.GetCurrentTime).AsKernelFunction()]);
#pragma warning restore SKEXP0001

// Kernel を作成
var kernel = builder.Build();

// Semantic Kernel の API で Function calling
var result = await kernel.InvokePromptAsync(
    "今日は何日？",
    new KernelArguments(new PromptExecutionSettings
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }));
Console.WriteLine(result.GetValue<string>());

// AI から呼ぶための関数を定義
class TimeTools
{
    [Description("Get the current local time.")]
    public static DateTimeOffset GetCurrentTime() => TimeProvider.System.GetLocalNow();
}
