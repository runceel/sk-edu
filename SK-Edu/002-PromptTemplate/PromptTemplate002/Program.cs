using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;

// テンプレートのファクトリを作成
IPromptTemplateFactory templateFactory = new AggregatorPromptTemplateFactory(
    // 複数のテンプレートファクトリを登録
    new LiquidPromptTemplateFactory(),
    new KernelPromptTemplateFactory(),
    new EchoPromptTemplateFactory());

// 何も指定していないので KernelPromptTemplateFactory がデフォルトで使用される
IPromptTemplate kernelTemplate = templateFactory.Create(
    new PromptTemplateConfig("""
        Kernel, {{$name}}!
        """));
// TemplateFormat が設定されているため LiquidPromptTemplateFactory が使用される
IPromptTemplate liquidTemplate = templateFactory.Create(
    new PromptTemplateConfig("""
        Liquid, {{name}}!
        """)
    {
        // テンプレートのフォーマットをLiquidに設定
        TemplateFormat = LiquidPromptTemplateFactory.LiquidTemplateFormat,
    });
// TemplateFormat が設定されているため LiquidPromptTemplateFactory が使用される
IPromptTemplate echoTemplate = templateFactory.Create(
    new PromptTemplateConfig("""
        Echo, {{name}}!
        """)
    {
        // 無効なテンプレートのフォーマットを設定
        TemplateFormat = "犬派×きのこ派",
    });

// テンプレートに渡す変数
var arguments = new KernelArguments
{
    ["name"] = "Kazuki",
};
// ダミーの空のカーネル
var kernel = new Kernel();

// レンダリングされたプロンプトを表示
Console.WriteLine($"""
    {await kernelTemplate.RenderAsync(kernel, arguments)}
    {await liquidTemplate.RenderAsync(kernel, arguments)}
    {await echoTemplate.RenderAsync(kernel, arguments)}
    """);
