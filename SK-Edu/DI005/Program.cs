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


var services = new ServiceCollection();
#pragma warning disable SKEXP0010 // preview なので警告が出るが無視する
services.AddAzureOpenAIChatClient(modelDeploymentName, endpoint, new AzureCliCredential());
#pragma warning restore SKEXP0010

var kernel = new Kernel(services.BuildServiceProvider());
var promptFunction = kernel.CreateFunctionFromPrompt("Hello");

// デフォルトの IChatClient が使われる
var result = await kernel.InvokeAsync(promptFunction);
Console.WriteLine(result.GetValue<string>());

