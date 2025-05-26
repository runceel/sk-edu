using Microsoft.SemanticKernel;

var kernel = new Kernel();
// 引数を受け取る関数を作成
var getFullNameFunction = kernel.CreateFunctionFromMethod(
    (string firstName, string lastName) => $"{firstName} {lastName}",
    "GetFullName");
// Kernel に関数をプラグインとして登録する
kernel.Plugins.AddFromFunctions("GreetingPlugin", [getFullNameFunction]);

// テンプレートのファクトリを作成
IPromptTemplateFactory templateFactory = new KernelPromptTemplateFactory();
// 関数を呼び出すテンプレートを作成
IPromptTemplate template = templateFactory.Create(new PromptTemplateConfig("""
    Hello, {{GreetingPlugin.GetFullName $firstName lastName='Ota'}}!
    """));

// テンプレートに変数を設定してレンダリング
string prompt = await template.RenderAsync(kernel, new KernelArguments
{
    ["firstName"] = "Kazuki",
});

// レンダリングされたプロンプトを表示
Console.WriteLine(prompt);


