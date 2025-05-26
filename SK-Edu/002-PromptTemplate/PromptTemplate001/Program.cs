using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;

// テンプレートのファクトリを作成
IPromptTemplateFactory templateFactory = new LiquidPromptTemplateFactory();
// テンプレートを作成
IPromptTemplate template = templateFactory.Create(
    new PromptTemplateConfig("""
        Hello, {{name}}!
        """)
    {
        // テンプレートのフォーマットをLiquidに設定
        TemplateFormat = LiquidPromptTemplateFactory.LiquidTemplateFormat,
    });

// テンプレートに変数を設定してレンダリング
string prompt = await template.RenderAsync(new Kernel(), new KernelArguments
{
    ["name"] = "Kazuki",
});

// レンダリングされたプロンプトを表示
Console.WriteLine(prompt);
