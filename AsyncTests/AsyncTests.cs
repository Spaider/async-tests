using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncTests
{
  [TestFixture]
  public class AsyncTests
  {
    [Test, Timeout(6000)]
    public async Task InterleavedTasksTest()
    {
      var tasks = TaskGenerator.CreateRandomDelayTasks(20);

      // Process each completed task one by one as soon as they completes
      foreach (var task in TaskUtils.Interleaved(tasks))
      {
        var result = await task;
        Console.WriteLine("Task {0} completed, delay was {1}",  result.ID, result.Delay);
      }
      // Note that tasks will appear 'almost' sorted by their delay
    }

    [Test]
    public void WhenAllOrFirstException_WithExceptionTest()
    {
      const int NUM_GOOD_TASKS = 5;
      var tasks = TaskGenerator.CreateRandomDelayTasks(NUM_GOOD_TASKS);
      tasks.Add(TaskGenerator.CreateFaultyTask(NUM_GOOD_TASKS, 2000));

      var withFaultyTask = TaskUtils.WhenAllOrFirstException(tasks);
      try
      {
        withFaultyTask.Wait();
        Assert.Fail("Faulty task did not reveal itself");
        // withFaultyTask.Result may be used in case all tasks ran to completion
      }
      catch (AggregateException)
      {
        Assert.That(withFaultyTask.IsFaulted);
        Console.WriteLine("Faulty task threw exception as expected");
      }
    }

    [Test]
    public async Task WhenAllOrFirstException_AllGoodTest()
    {
      const int NUM_GOOD_TASKS = 5;
      var tasks = TaskGenerator.CreateRandomDelayTasks(NUM_GOOD_TASKS);
      var allGoodTask = TaskUtils.WhenAllOrFirstException(tasks);

      // Note how to fetch wrapped tasks results
      foreach (var taskParam in await allGoodTask)
      {
        Console.WriteLine("Task {0} completed in {1} ms", taskParam.ID, taskParam.Delay);
      }
    }

    [Test]
    public async Task AsyncCacheTest()
    {
      var cache = new AsyncCache<int, TaskGenerator.TaskParams>(i => TaskGenerator.CreateDelayTask(i, 1000));

      var sw = Stopwatch.StartNew();
      var result = await cache[123];
      Assert.That(result.ID == 123);
      Console.WriteLine("Not cached result in {0}ms", sw.ElapsedMilliseconds);

      sw = Stopwatch.StartNew();
      result = await cache[123];
      Assert.That(result.ID == 123);
      Console.WriteLine("Cached result in {0}ms", sw.ElapsedMilliseconds);
    }

    [Test, ExpectedException(typeof(ApplicationException))]
    public async Task RetryOnFault_Fail()
    {
      var badFn = new Func<Task<int>>(() =>
      {
        Console.WriteLine("Trying...");
        throw new ApplicationException("Bad luck");
      });
      var task = TaskUtils.RetryOnFault(badFn, 3);
      await task;
    }   
    
    [Test]
    public async Task RetryOnFault_Success()
    {
      var i = 0;
      var clumsyFn = new Func<Task<int>>(() => Task.Run(() =>
      {
        Console.WriteLine("Trying...");
        i++;

        if (i < 2)
          throw new ApplicationException("Bad luck");

        Console.WriteLine("Success");
        return i;
      }));
      var task = TaskUtils.RetryOnFault(clumsyFn, 3);
      var result = await task;
      Assert.AreEqual(result, 2);
    }
  }
}
