using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// AOAI にアクセスする IChatClient を登録する
#pragma warning disable SKEXP0010 
builder.Services.AddAzureOpenAIChatClient(
    builder.Configuration["AOAI:ModelDeploymentName"]!,
    new AzureOpenAIClient(new(builder.Configuration["AOAI:Endpoint"]!),
    new AzureCliCredential()));
#pragma warning restore SKEXP0010 
// Kernel を DI コンテナに登録する
builder.Services.AddKernel();

// Kernel のプラグインを登録する
builder.Services.AddSingleton(sp =>
    KernelPluginFactory.CreateFromFunctions("AI",
    [
        KernelFunctionFactory.CreateFromPrompt(promptConfig: new("""
            <message role="system">
              あなたは猫型アシスタントです。ユーザーの問題解決を行ってください。
              猫らしく振舞うために語尾は「にゃん」にしてください。
            </message>
            <message>
              {{$message}}
            </message>
            """)
        {
            Name = "InvokeCat",
        }),
    ]));

var app = builder.Build();

app.MapGet("/ai", async ([FromQuery]string message, [FromServices]Kernel kernel) =>
{
    // DI コンテナから Kernel を取して処理を行う
    var result = await kernel.InvokeAsync(
        "AI", 
        "InvokeCat",
        arguments: new()
        {
            ["message"] = message,
        });
    return result.GetValue<string>();
});

app.Run();
