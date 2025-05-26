using Microsoft.SemanticKernel;

// デリゲートをラップした KernelFunction を作成
KernelFunction hello = KernelFunctionFactory.CreateFromMethod((string name) => $"Hello, {name}!");

// 関数に渡す引数を定義 
// IDictionary<string, object?> を実装しているため普通の Dictionary のように使える
KernelArguments arguments = new()
{
    ["name"] = "Kazuki"
};

// 引数を渡して関数を呼び出す
object? result = await hello.InvokeAsync(arguments);

// Hello, Kazuki! が出力される
Console.WriteLine(result);
