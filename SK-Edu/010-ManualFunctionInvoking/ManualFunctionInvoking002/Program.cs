using System.ComponentModel;
using System.Text.Json;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using FunctionCallContent = Microsoft.SemanticKernel.FunctionCallContent;
using FunctionResultContent = Microsoft.SemanticKernel.FunctionResultContent;

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
var state = JsonSerializer.Serialize(new ChatHistory
{
    new (AuthorRole.System, "あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。"),
    new (AuthorRole.User, "今日の東京の天気を教えて"),
});
IEnumerable<FunctionCallContent> functionCalls = [];
do
{
    // 1 ターン進める
    (state, functionCalls) = await AdvanceTurnAsync(chatCompletion, state, kernel);

    if (functionCalls.Any())
    {
        // ツール呼び出しがある場合は、ユーザーに確認してから実行する
        var restoredMessages = JsonSerializer.Deserialize<ChatHistory>(state)
            ?? throw new InvalidOperationException("State is not a valid ChatHistory.");
        foreach (var functionCall in functionCalls)
        {
            // ツール呼び出しの内容を表示
            var functionArgs = string.Join(", ", functionCall.Arguments?.Select(x => $"{x.Key}: {x.Value}") ?? []);
            Console.WriteLine($"この処理を呼び出しても良いですか？(y/n): {functionCall.FunctionName}({functionArgs})");
            var userInput = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "n";

            if (userInput == "y")
            {
                // ユーザーが承認した場合は関数を呼び出す
                var functionResult = await functionCall.InvokeAsync(kernel);
                restoredMessages.Add(functionResult.ToChatMessage());
            }
            else
            {
                // ユーザーが拒否した場合は、拒否メッセージを追加する
                var rejectedMessage = new FunctionResultContent(functionCall, "Rejected by user.");
                restoredMessages.Add(rejectedMessage.ToChatMessage());
                state = JsonSerializer.Serialize(restoredMessages);
            }
        }

        // チャット履歴を更新
        state = JsonSerializer.Serialize(restoredMessages);
    }
} while (functionCalls.Any());

// 最終回答を表示
var finalMessages = JsonSerializer.Deserialize<ChatHistory>(state)
    ?? throw new InvalidOperationException("State is not a valid ChatHistory.");
Console.WriteLine(finalMessages.Last().Content);


static async Task<(string State, IEnumerable<FunctionCallContent> FunctionCalls)> AdvanceTurnAsync(
    IChatCompletionService chatCompletion, 
    string state, 
    Kernel kernel)
{
    // チャット履歴を復元
    var messages = JsonSerializer.Deserialize<ChatHistory>(state)
        ?? throw new InvalidOperationException("State is not a valid ChatHistory.");
    // チャットを進める
    var response = await chatCompletion.GetChatMessageContentAsync(
        messages,
        new PromptExecutionSettings
        {
            // 関数の選択は行うが自動呼出しは行わない
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false),
        },
        kernel);
    messages.Add(response);

    // チャット履歴と関数呼び出しの有無を返す
    return (
        State: JsonSerializer.Serialize(messages),
        FunctionCalls: FunctionCallContent.GetFunctionCalls(response)
    );
}


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
