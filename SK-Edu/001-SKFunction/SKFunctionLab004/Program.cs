using Microsoft.SemanticKernel;

KernelFunction f = KernelFunctionFactory.CreateFromMethod(
    // Kernel を引数に受け取れる
    (Kernel kernel, string name) => $"Hello, {name} from {kernel}!");

Kernel kernel = new();

// KernelArguments では name 引数だけを渡す
var result = await f.InvokeAsync(kernel, new KernelArguments
{
    ["name"] = "Kazuki"
});

// 結果を表示
Console.WriteLine(result.GetValue<string>());