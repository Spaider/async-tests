using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncTests
{
  public static class TaskUtils
  {
    /// <summary>
    /// Makes it easy to process every completed task after its completion especially when 
    /// number of tasks is huge. Simplify interleaving scenario and allows this simple code pattern:
    /// <code>
    ///   var tasks = Interleaved(...);
    ///   foreach(var task in tasks)
    ///   {
    ///     // Process completed task
    ///     ...
    ///   }
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of tasks to process</typeparam>
    /// <param name="tasks">Tasks to configure for interleaved processing scenario</param>
    /// <returns>Tasks configured for interleaved processing scenario</returns>
    /// <remarks>
    /// From http://www.microsoft.com/en-us/download/details.aspx?id=19957 document:
    /// There is a potential performance problem with using Task.WhenAny to support an interleaving 
    /// scenario when using very large sets of tasks. Every call to WhenAny will result in 
    /// a continuation being registered with each task, which for N tasks will amount to O(N^2) 
    /// continuations created over the lifetime of the interleaving operation.</remarks>
    public static IEnumerable<Task<T>> Interleaved<T>(IEnumerable<Task<T>> tasks)
    {
      var inputTasks = tasks.ToList();
      var sources = (from _ in Enumerable.Range(0, inputTasks.Count)
                     select new TaskCompletionSource<T>()).ToList(); 
      var nextTaskIndex = -1;

      foreach (var inputTask in inputTasks)
      {
        inputTask.ContinueWith(completed =>
        {
          var source = sources[Interlocked.Increment(ref nextTaskIndex)];

          if (completed.IsFaulted)
          {
            Debug.Assert(completed.Exception != null);
            source.TrySetException(completed.Exception.InnerExceptions);
          }
          else if (completed.IsCanceled)
          {
            source.TrySetCanceled();
          }
          else
          {
            source.TrySetResult(completed.Result);
          }
        }, 
        CancellationToken.None,
        TaskContinuationOptions.ExecuteSynchronously, 
        TaskScheduler.Default);
      }
      return from source in sources
             select source.Task;
    }

    /// <summary>
    /// Creates task that completes when all input tasks run to completion or
    /// throws an exception if one of the tasks failed
    /// </summary>
    /// <typeparam name="T">Type of tasks to process</typeparam>
    /// <param name="tasks">Tasks to wrap</param>
    /// <returns>Task which either completes successfully if all container tasks run fine or
    /// throws an exception when first task fails.</returns>
    public static Task<T[]> WhenAllOrFirstException<T>(IEnumerable<Task<T>> tasks)
    {
      var inputs = tasks.ToList();
      var ce = new CountdownEvent(inputs.Count); 
      var tcs = new TaskCompletionSource<T[]>();

      Action<Task> onCompleted = completed =>
      {
        if (completed.IsFaulted)
        {
          Debug.Assert(completed.Exception != null);
          tcs.TrySetException(completed.Exception.InnerExceptions);
        }
        if (ce.Signal() && !tcs.Task.IsCompleted)
        {
          tcs.TrySetResult(inputs.Select(t => t.Result).ToArray());
        }
      };
      foreach (var t in inputs)
      {
        t.ContinueWith(onCompleted);
      }
      return tcs.Task;
    }

    /// <summary>
    /// Tries to call specified function given amount of times, silently swallowing exception
    /// untils tries are exhausted
    /// </summary>
    /// <typeparam name="TResult">Type of function return value</typeparam>
    /// <param name="function">Function to be called</param>
    /// <param name="maxTries">Number of tries to call function until give up</param>
    /// <returns>Return value from successfully called function</returns>
    public static async Task<TResult> RetryOnFault<TResult>(Func<Task<TResult>> function, int maxTries)
    {
      if (maxTries <= 0)
        throw new ArgumentException("Number of tries must be positive", "maxTries");

      for (var i = 0; i < maxTries; i++)
      {
        try
        {
          return await function().ConfigureAwait(false);
        }
        catch
        {
          if (i == maxTries - 1) throw;
        }
      }
      return default(TResult);
    }
  }
}