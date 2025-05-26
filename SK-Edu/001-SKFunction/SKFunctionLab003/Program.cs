using Microsoft.SemanticKernel;

// 複数引数の関数を定義
KernelFunction add = KernelFunctionFactory.CreateFromMethod((int x, int y) => x + y);

// 関数に渡す引数を定義 
KernelArguments arguments = new()
{
    ["x"] = 5,
    ["y"] = 10
};

// Kernel を作成
var kernel = new Kernel();

// Kernel を渡す場合は FunctionResult が返ってくる
FunctionResult result1 = await add.InvokeAsync(kernel, arguments);
// GetValue で戻り値を取得
// 15 が出力される
Console.WriteLine(result1.GetValue<int>());
