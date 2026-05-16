using Microsoft.AspNetCore.Components.Web;
using Verso.Blazor.Components.Pages;

namespace Verso.Blazor.Shared.Tests;

[TestClass]
public sealed class NotebookPageExecutionTests : BunitTestContext
{
    [TestMethod]
    public async Task RunButton_ReturnsBeforeExecutionCompletes()
    {
        TestContext!.JSInterop.Mode = JSRuntimeMode.Loose;

        var cell = new CellModel
        {
            Type = "code",
            Language = "powershell",
            Source = "Read-Host 'Name'"
        };
        var executionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseExecution = new TaskCompletionSource<ExecutionResultDto>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeNotebookService
        {
            IsLoaded = true,
            Cells = new List<CellModel> { cell },
            RegisteredLanguages = new List<KernelLanguageInfo>
            {
                new("powershell", "PowerShell")
            },
            ExecuteCellAsyncHandler = _ =>
            {
                executionStarted.TrySetResult();
                return releaseExecution.Task;
            }
        };
        TestContext.Services.AddSingleton<INotebookService>(service);

        var cut = RenderComponent<NotebookPage>();
        var clickTask = cut.Find("button.verso-cell-btn--run").ClickAsync(new MouseEventArgs());

        var completed = await Task.WhenAny(clickTask, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.AreSame(clickTask, completed, "Run click should return control before cell execution completes.");

        await clickTask;
        await WaitForAsync(executionStarted.Task, "Expected background execution to start.");
        Assert.IsFalse(releaseExecution.Task.IsCompleted, "Test setup should still be holding execution open.");

        releaseExecution.SetResult(new ExecutionResultDto(cell.Id, "Success", 1, TimeSpan.Zero));
    }

    private static async Task WaitForAsync(Task task, string failureMessage)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(task, completed, failureMessage);
        await task;
    }
}
