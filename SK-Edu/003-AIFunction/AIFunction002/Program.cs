
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;

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
    // Liquid テンプレートはループや分岐をサポートしているので複数のタグを生成可能
    new PromptTemplateConfig("""
        <message role="system">
          あなたは猫型アシスタントです。
          猫らしく振舞うために語尾は「にゃん」にしてください。
        </message>
        {% for message in messages %}
        <message role="{{message.role}}">
            {{message.text}}
        </message>
        {% endfor %}
        """)
    {
        TemplateFormat = LiquidPromptTemplateFactory.LiquidTemplateFormat,
    },
    // テンプレートファクトリを指定して Liquid テンプレートを使用する
    promptTemplateFactory: new LiquidPromptTemplateFactory());

// パラメーターとして渡す ChatMessage の配列を作成
ChatMessage[] messages = [
    new (ChatRole.User, "こんにちは！私の名前は Kazuki です。どうぞよろしくお願いします。"),
    new (ChatRole.Assistant, "こんにちは、Kazukiさん！よろしくお願いします。にゃん！"),
    new (ChatRole.User, "実は私は犬なんです…、黙っててごめんなさい…。猫の敵です…。"),
];

// テンプレートに変数を設定してレンダリング
var renderedPrompt = await introductionFunction.InvokeAsync(
    kernel,
    new KernelArguments
    {
        ["messages"] = messages,
    });
// レンダリングされたプロンプトを表示
Console.WriteLine(renderedPrompt);

