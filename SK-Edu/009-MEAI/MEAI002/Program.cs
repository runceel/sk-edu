using System.Text.Json;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
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

// AOAI 用の Chat Client を登録
builder.AddAzureOpenAIChatClient(
    modelDeploymentName,
    endpoint,
    new AzureCliCredential());

// AI サービスが登録された Kernel を作成
var kernel = builder.Build();

// プロンプトから関数を作成 (実行するには AI サービスの登録された Kernel が必要)
var greetingFunction = KernelFunctionFactory.CreateFromPrompt("こんにちは");

// WithKernel メソッドを使うと Kernel を持った関数の Clone を作成できる
#pragma warning disable SKEXP0001
await InvokeAIFunctionAndWriteOutputAsync(greetingFunction.WithKernel(kernel));
#pragma warning restore SKEXP0001

async Task InvokeAIFunctionAndWriteOutputAsync(AIFunction function)
{
    var result = await function.InvokeAsync();
    // 結果は JSON 形式で返されるので、デシリアライズする
    // デシリアライズの時のオプションは Microsoft.Extensions.AI でデフォルトで使われるオプションが
    // 内部でも使われているので、それを指定する
    var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
        (JsonElement) result!, 
        AIJsonUtilities.DefaultOptions);
#pragma warning disable SKEXP0001 // AsKernelFunction はプレビュー機能
    // KernelFunction と戻り値を使って FunctionResult を作成
    var functionResult = new FunctionResult(function.AsKernelFunction(), chatResponse);
#pragma warning restore SKEXP0001
    // FunctionResult から値を取得
    var answer = functionResult.GetValue<string>();
    // 結果を出力
    Console.WriteLine(answer);
}

