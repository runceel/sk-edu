using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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

// Kernel を作成
var kernel = builder.Build();

// Kernel のサービスから IChatClient を取得
var chatClient = kernel.Services.GetRequiredService<IChatClient>();

List<ChatMessage> messages;
if (File.Exists("chatlog.json"))
{
    // chatlog.json が存在する場合は、そこからメッセージを読み込む
    await using var stream = File.OpenRead("chatlog.json");
    messages = await JsonSerializer.DeserializeAsync<List<ChatMessage>>(stream)
        ?? throw new InvalidOperationException("Failed to deserialize chat log.");
}
else
{
    // chatlog.json が存在しない場合は、初期メッセージを設定
    messages = [new ChatMessage(ChatRole.System, "あなたは猫型アシスタントです。猫らしく振舞うために語尾は「にゃん」にしてください。")];
}

// ユーザーからの入力を受け付ける
Console.Write("User > ");
var userInput = Console.ReadLine();
if (string.IsNullOrWhiteSpace(userInput))
{
    Console.WriteLine("No input provided. Exiting.");
    return;
}

// ユーザーの入力をメッセージに追加し、AIからの応答を取得
messages.Add(new ChatMessage(ChatRole.User, userInput));
var response = await chatClient.GetResponseAsync(messages);
Console.WriteLine($"AI > {response.Text}");

// AIの応答をメッセージに追加し、chatlog.jsonに保存
messages.AddRange(response.Messages);
await using var fileStream = File.Create("chatlog.json");
await JsonSerializer.SerializeAsync(fileStream, messages, new JsonSerializerOptions
{
    // 日本語がエスケープされないように、UnicodeRanges.All を指定
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    // インデントを有効にして、読みやすい形式で保存
    WriteIndented = true,
});

