using Microsoft.SemanticKernel;

// デリゲートをラップした KernelFunction を作成
KernelFunction hello = KernelFunctionFactory.CreateFromMethod((string name) => $"Hello, {name}!");

// 関数に渡す引数を定義 
// IDictionary<string, object?> を実装しているため普通の Dictionary のように使える
KernelArguments arguments = new()
{
    ["name"] = "Kazuki"
};

// Kernel を作成
var kernel = new Kernel();

// Kernel を渡す場合は FunctionResult が返ってくる
FunctionResult result1 = await hello.InvokeAsync(kernel, arguments);
// GetValue で戻り値を取得
// Hello, Kazuki! が出力される
Console.WriteLine(result1.GetValue<string>());

// Kernel から関数を呼び出すことも可能
FunctionResult result2 = await kernel.InvokeAsync(hello, arguments);
// GetValue で戻り値を取得
// Hello, Kazuki! が出力される
Console.WriteLine(result1.GetValue<string>());
