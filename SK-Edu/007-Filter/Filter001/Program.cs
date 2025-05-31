using Azure.Identity;
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

// フィルターを登録
builder.Services.AddSingleton<IFunctionInvocationFilter, HumanInTheLoopFilter>();

// AOAI 用の Chat Client を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());

// AI サービスが登録された Kernel を作成
var kernel = builder.Build();

// Foo 関数と Bar 関数を作成
var fooFunction = kernel.CreateFunctionFromMethod(
    () =>
    {
        Console.WriteLine("Foo 関数が呼ばれました。");
        return "Foo result";
    },
    functionName: "Foo");
var barFunction = kernel.CreateFunctionFromMethod(
    () =>
    {
        Console.WriteLine("Bar 関数が呼ばれました。");
        return "Bar result";
    },
    functionName: "Bar");

// Foo 関数を呼び出す。フィルターで確認が求められる。
var fooResult = await fooFunction.InvokeAsync(kernel);
Console.WriteLine($"Foo result: {fooResult.GetValue<string>()}");

// Bar 関数を呼び出す。フィルターで確認は求められない。
var barResult = await barFunction.InvokeAsync(kernel);
Console.WriteLine($"Bar result: {barResult.GetValue<string>()}");

public class HumanInTheLoopFilter : IFunctionInvocationFilter
{
    public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Name == "Foo")
        {
            if (!GetUserApproval())
            {
                context.Result = new FunctionResult(context.Result, "ユーザーによりキャンセルされました。");
                return Task.CompletedTask;
            }
        }

        return next(context);
    }

    private static bool GetUserApproval()
    {
        // ここで人間に確認を求める処理を実装する
        Console.WriteLine("Foo 関数の実行を続けますか？ (yes/no)");
        return Console.ReadLine()?.Trim()?.ToLower() == "yes";
    }
}
