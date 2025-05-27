using System.ComponentModel;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

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

// 固定の日時を返す TimeProvider の実装を DI コンテナに登録
builder.Services.AddSingleton<TimeProvider, FixedTimeProvider>();
// Kernel にプラグインを登録
builder.Plugins.AddFromType<TimePlugin>();

// AOAI 用の Chat Client を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());

// Kernel を作成
var kernel = builder.Build();

// プロンプトの実行時のパラメーター
var promptExecutionSettings = new AzureOpenAIPromptExecutionSettings
{
    // 関数は自動で呼び出す
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    // AOAI の gpt シリーズおなじみの温度パラメーターを設定
    Temperature = 0,
};

// 関数の自動呼出しを有効にしたプロンプトを定義して、プラグインの関数を AI に呼び出してもらう
var result = await kernel.InvokePromptAsync("""
    <message role="system">
      あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。
    </message>
    <message role="user">
      {{$userInput}}
    </message>
    """,
    // KernelArguments のコンストラクタに実行設定を渡す
    new KernelArguments(promptExecutionSettings)
    {
        ["userInput"] = "今日は何日ですか？",
    });

// 結果を表示
Console.WriteLine(result.GetValue<string>());

// クラスでプラグインを定義
// TimeProvider は DI コンテナから取得
[Description("A plugin that provides time-related functions.")]
class TimePlugin(TimeProvider timeProvider)
{
    [KernelFunction, Description("Get the current local time.")]
    [return: Description("The current local time as a DateTimeOffset object.")]
    public DateTimeOffset GetLocalNow() => timeProvider.GetLocalNow();
}

// 固定の日時を返す TimeProvider の実装
class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _specificDateTime =
        new(1969, 7, 20, 20, 17, 40, TimeZoneInfo.Utc.BaseUtcOffset);

    public override DateTimeOffset GetUtcNow() => _specificDateTime;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
}

