using Schedule.EnumSpace;
using Schedule.Runners;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
  public class RunnerTests
  {
    [Fact]
    public void Runner()
    {
      var context = SynchronizationContext.Current;

      var scheduler = new BackgroundRunner();
      var x1 = Environment.CurrentManagedThreadId;
      var x2 = scheduler.Send(() => Environment.CurrentManagedThreadId).Task.Result;
      var x3 = Task.Run(() => scheduler.Send(() => Environment.CurrentManagedThreadId).Task.Result).Result;
      var x4 = Task.Factory.StartNew(() => scheduler.Send(() => Environment.CurrentManagedThreadId).Task.Result).Result;
      var x5 = Task.Run(async () => await scheduler.Send(() => Environment.CurrentManagedThreadId).Task).Result;
      //var x6 = scheduler.Send(Task.Run(() => Environment.CurrentManagedThreadId)).Task.Result;
      var x6 = scheduler.Send(() => Task.Factory.StartNew(() => Environment.CurrentManagedThreadId, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext())).Task.Result;

      var x7 = Task.Run(() => Environment.CurrentManagedThreadId).Result;

      Assert.NotEqual(x1, x2.Data);
      Assert.Equal(x2.Data, x3.Data);
      Assert.Equal(x3.Data, x4.Data);
      Assert.Equal(x4.Data, x5.Data);
      Assert.Equal(x5.Data, x6.Data);
    }

    [Fact]
    public void Access()
    {
      var exceptions = 0;
      var count = 1000000;
      var items = Enumerable.Range(1, count).ToList();
      var scheduler = new BackgroundRunner(Environment.ProcessorCount);

      try
      {
        Task.WaitAll(
          scheduler.Send(() => { for (var i = 0; i < count; i++) items.Add(i); }).Task,
          scheduler.Send(() => { for (var i = 1; i < count; i++) items[i] = items[i - 1] + items.Count; }).Task
        );
      }
      catch (Exception)
      {
        exceptions++;
      }

      Assert.Equal(0, exceptions);
    }

    [Fact]
    public void InputPriority()
    {
      var value = -1;
      var processes = new List<Task>();
      var scheduler = new BackgroundRunner(1) { Count = 1 };

      processes.Add(scheduler.Send(() => value = 1).Task);
      processes.Add(scheduler.Send(() => value = 2).Task);
      processes.Add(scheduler.Send(() => value = 3).Task);

      Task.WaitAll(processes.ToArray());

      Assert.Equal(3, value);

      value = -1;
      scheduler.Count = 5;

      processes.Add(scheduler.Send(() => value = 1).Task);
      processes.Add(scheduler.Send(() => value = 2).Task);
      processes.Add(scheduler.Send(() => value = 3).Task);

      Task.WaitAll(processes.ToArray());

      Assert.Equal(3, value);
    }

    [Fact]
    public void ProcessPriority()
    {
      var value = -1;
      var processes = new List<Task>();
      var scheduler = new BackgroundRunner(1) { Count = 1, Precedence = PrecedenceEnum.Current };

      processes.Add(scheduler.Send(() => value = 1).Task);
      processes.Add(scheduler.Send(() => value = 2).Task);
      processes.Add(scheduler.Send(() => value = 3).Task);

      Task.WaitAll(processes.ToArray());

      Assert.Equal(1, value);

      value = -1;
      scheduler.Count = 5;

      processes.Add(scheduler.Send(() => value = 1).Task);
      processes.Add(scheduler.Send(() => value = 2).Task);
      processes.Add(scheduler.Send(() => value = 3).Task);

      Task.WaitAll(processes.ToArray());

      Assert.Equal(3, value);
    }
  }
}
