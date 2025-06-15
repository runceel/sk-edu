using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using FunctionCallContent = Microsoft.SemanticKernel.FunctionCallContent;

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

// Kernel にプラグインを登録
builder.Plugins.AddFromType<TimePlugin>();
builder.Plugins.AddFromType<WeatherPlugin>();

// AOAI 用の Chat Completion を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());

// Kernel を作成
var kernel = builder.Build();

#pragma warning disable SKEXP0001
// IChatClient を IChatCompletionService に変換（プレビュー機能なので警告の抑止が必要）
var chatCompletion = kernel.GetRequiredService<IChatClient>().AsChatCompletionService();
#pragma warning restore SKEXP0001


// GetLocalNow と GetWeather を呼び出してもらうためのメッセージを作成
ChatHistory messages = [
        new (AuthorRole.System, "あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。"),
        new (AuthorRole.User, "今日の東京の天気を教えて"),
    ];

IEnumerable<FunctionCallContent> functionCalls = [];
do
{
    // ツール呼び出しが無くなるまでループ
    var response = await chatCompletion.GetChatMessageContentAsync(
        messages,
        new PromptExecutionSettings
        {
            // 関数の選択は行うが自動呼出しは行わない
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false),
        },
        kernel);
    messages.Add(response);
    functionCalls = FunctionCallContent.GetFunctionCalls(response);
    if (functionCalls.Any())
    {
        // ツール呼び出しのメッセージを表示
        foreach (var functionCall in functionCalls)
        {
            // ツール呼び出しの内容を表示
            var functionArgs = string.Join(", ", functionCall.Arguments?.Select(x => $"{x.Key}: {x.Value}") ?? []);
            Console.WriteLine($"Function call: {functionCall.PluginName}-{functionCall.FunctionName}({functionArgs})");
            var functionResult = await functionCall.InvokeAsync(kernel);
            messages.Add(functionResult.ToChatMessage());
        }
    }
} while (functionCalls.Any());

// チャット履歴を全て表示
Console.WriteLine(JsonSerializer.Serialize(messages, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
}));



// クラスでプラグインを定義
[Description("A plugin that provides time-related functions.")]
class TimePlugin
{
    [KernelFunction, Description("Get the current local time.")]
    [return: Description("The current local time as a DateTimeOffset object.")]
    public DateTimeOffset GetLocalNow() =>
        TimeProvider.System.GetLocalNow();
}

class WeatherPlugin
{
    [KernelFunction, Description("Get the current weather for a given location.")]
    [return: Description("The current weather as a string.")]
    public string GetWeatherByDate(
        [Description("The date and time for which to get the weather.")]
        DateTimeOffset dateTime,
        [Description("The location for which to get the weather.")]
        string location)
    {
        return $"The weather in {location} on {dateTime:yyyy-MM-dd} is sunny with a high of 25°C.";
    }
}
