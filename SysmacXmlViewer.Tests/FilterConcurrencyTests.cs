using System.Collections.Concurrent;
using System.Reflection;
using SysmacXmlViewer.Models;
using SysmacXmlViewer.ViewModels;

namespace SysmacXmlViewer.Tests;

public class FilterConcurrencyTests
{
    // Hold UI continuations until a new backup/filter supersedes the completed worker.
    private sealed class QueuedContext : SynchronizationContext
    {
        public readonly BlockingCollection<Action> Queue = new();
        public override void Post(SendOrPostCallback callback, object? state) => Queue.Add(() => callback(state));
    }

    private static Task Filter(MainViewModel vm) => (Task)typeof(MainViewModel)
        .GetMethod("ApplyFiltersAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null)!;

    private static void SetVariables(MainViewModel vm, List<VariableItem> values) => typeof(MainViewModel)
        .GetField("_allVariables", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, values);

    [Fact]
    public void PouFilterCombinesWithSearchAndClearsHiddenSelection()
    {
        var previous = SynchronizationContext.Current;
        var context = new QueuedContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var vm = new MainViewModel();
            var global = new VariableItem { Name = "VAR:/Shared", Comment = "Target" };
            var local = new VariableItem { Name = "VAR://22222222222242228222222222222222/Shared", Comment = "Target" };
            SetVariables(vm, new() { global, local });
            vm.SelectedVariable = local;
            vm.FilterText = "Target";
            Assert.True(context.Queue.TryTake(out var first, 5000));
            first!();
            vm.SelectedPou = new("Global", "Global");
            Assert.True(vm.IsBusy);
            Assert.True(context.Queue.TryTake(out var next, 5000));
            next!();
            Assert.Same(global, Assert.Single(vm.FilteredVariables));
            Assert.Null(vm.SelectedVariable);
            Assert.False(vm.IsBusy);
            Assert.Equal("Ready", vm.ActivityStatus);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    [Fact]
    public void CompletedOldSearchCannotReplaceNewBackup()
    {
        var previous = SynchronizationContext.Current;
        var context = new QueuedContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var vm = new MainViewModel();
            var old = new List<VariableItem> { new() { Name = "Old", DataType = "INT" } };
            SetVariables(vm, old);
            var first = Filter(vm);
            Assert.True(context.Queue.TryTake(out var oldContinuation, 5000));
            typeof(MainViewModel).GetMethod("ClearMemory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(vm, null);
            var current = new VariableItem { Name = "New", DataType = "INT" };
            SetVariables(vm, new() { current });
            var second = Filter(vm);
            Assert.True(context.Queue.TryTake(out var newContinuation, 5000));
            newContinuation!();
            oldContinuation!();
            Assert.True(first.IsCompletedSuccessfully);
            Assert.True(second.IsCompletedSuccessfully);
            Assert.Same(current, Assert.Single(vm.FilteredVariables));
            Assert.Single(old);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }

    [Fact]
    public void LatestSearchWinsEvenWhenOldContinuationRunsLast()
    {
        var previous = SynchronizationContext.Current;
        var context = new QueuedContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var vm = new MainViewModel();
            SetVariables(vm, new() { new() { Name = "Alpha" }, new() { Name = "Beta" } });
            vm.FilterText = "Alpha";
            Assert.True(context.Queue.TryTake(out var oldContinuation, 5000));
            vm.FilterText = "Beta";
            Assert.True(context.Queue.TryTake(out var newContinuation, 5000));
            newContinuation!();
            oldContinuation!();
            Assert.Equal("Beta", Assert.Single(vm.FilteredVariables).Name);
        }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
