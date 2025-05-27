using Azure.Identity;
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

// プロンプトから関数を作成
var introductionFunction = KernelFunctionFactory.CreateFromPrompt(
    new PromptTemplateConfig("""
        <message role="system">
          あなたは猫型アシスタントです。
          猫らしく振舞うために語尾は「にゃん」にしてください。
        </message>
        <message role="user">
          <text>
            こんにちは！私の名前は {{$name}} です。
            どうぞよろしくお願いします。
            この画像に写っている動物はなんですか？
          </text>
          <image>https://pbs.twimg.com/profile_images/1642066284379799552/VCZ4d9Hw_400x400.jpg</image>
        </message>
        """)
    {
        InputVariables = [
            // name パラメーターでエスケープ処理を無効にする
            new() { Name = "name", AllowDangerouslySetContent = true },
        ]
    });

// 実行して結果を表示
var result = await introductionFunction.InvokeAsync(
    kernel, 
    new KernelArguments
    {
        ["name"] = "Kazuki",
    });
Console.WriteLine(result.GetValue<string>());
