using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AsyncTests
{
  public static class TaskGenerator
  {
    public struct TaskParams
    {
      public int ID    { get; set; }
      public int Delay { get; set; }
    }

    /// <summary>
    /// Creates task which asynchronosuly waits for given number of milliseconds
    /// </summary>
    /// <param name="taskID">Arbitrary task ID meaningful for the caller</param>
    /// <param name="delay">Delay in milliseconds</param>
    /// <returns>Task that asynchronosuly waits for given number of milliseoncs</returns>
    public static async Task<TaskParams> CreateDelayTask(int taskID, int delay)
    {
      await Task.Delay(delay);
      return new TaskParams
      {
        ID = taskID, 
        Delay = delay
      };
    }

    /// <summary>
    /// Creates list of tasks with random delays
    /// </summary>
    /// <param name="count">Number of tasks to create</param>
    /// <param name="maxDelay">Minimum task delay</param>
    /// <param name="minDelay">Maximum task delay</param>
    /// <returns>List of tasks with random delays</returns>
    public static List<Task<TaskParams>> CreateRandomDelayTasks(int count, int maxDelay = 5000, int minDelay = 100)
    {
      var tasks = new List<Task<TaskGenerator.TaskParams>>(count);
      var rnd = new Random((int)DateTime.Now.Ticks);
      var maxDuration = maxDelay - minDelay;

      for (var i = 0; i < count; i++)
      {
        tasks.Add(TaskGenerator.CreateDelayTask(i, rnd.Next(maxDuration) + minDelay));
      }

      return tasks;
    }

    /// <summary>
    /// Creates task that asynchonosuly waits for requested milliseconds and then fails
    /// with exception
    /// </summary>
    /// <param name="taskID">Arbitrary task ID meaningful for the caller</param>
    /// <param name="delay">Delay in milliseconds</param>
    /// <returns></returns>
    public static async Task<TaskParams> CreateFaultyTask(int taskID, int delay)
    {
      await Task.Delay(delay);
      throw new Exception("Task failed");
    }     

    
  }
}