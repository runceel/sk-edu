// User Secrets から設定を読み込む
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

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

// フィルターを登録
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, LoggingAutoFunctionInvocationFilter>();
builder.Services.AddSingleton<IAutoFunctionInvocationFilter, HumanInTheLoopAutoFunctionInvocationFilter>();

// 現在時間を取得するプラグイン
builder.Plugins.AddFromFunctions("TimePlugin",
    [
        KernelFunctionFactory.CreateFromMethod(
            () => TimeProvider.System.GetLocalNow(),
            functionName: "GetLocalNow",
            description: "現在のローカル時間を取得します。"),
    ]);

// AOAI 用の Chat Client を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());

// AI サービスが登録された Kernel を作成
var kernel = builder.Build();

// 時間を聞いて自動関数呼び出しをしてもらう
var result = await kernel.InvokePromptAsync(
    "今何時？",
    arguments: new(new PromptExecutionSettings
    {
        // 自動関数呼び出しをオンに設定
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }));
// 結果を出力
Console.WriteLine(result.GetValue<string>());

// ログをとるフィルター
class LoggingAutoFunctionInvocationFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context, 
        Func<AutoFunctionInvocationContext, Task> next)
    {
        Console.WriteLine($"開始: {nameof(LoggingAutoFunctionInvocationFilter)}#{nameof(OnAutoFunctionInvocationAsync)}({context.Function.Name})");
        await next(context);
        Console.WriteLine($"終了: {nameof(LoggingAutoFunctionInvocationFilter)}#{nameof(OnAutoFunctionInvocationAsync)}({context.Function.Name})");
    }
}

// 人間の確認をおこなうフィルター
class HumanInTheLoopAutoFunctionInvocationFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        if (!GetUserApproval(context.Function.Name))
        {
            context.Result = new FunctionResult(context.Function, "ユーザーによりキャンセルされました。");
            return;
        }

        await next(context);
    }

    private static bool GetUserApproval(string functionName)
    {
        // ここで人間に確認を求める処理を実装する
        Console.WriteLine($"{functionName} 関数の実行を続けますか？ (yes/no)");
        return Console.ReadLine()?.Trim()?.ToLower() == "yes";
    }
}
