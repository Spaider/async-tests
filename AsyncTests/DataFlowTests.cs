using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using NUnit.Framework;

namespace AsyncTests
{
  [TestFixture]
  public class DataFlowTests
  {
    [Test]
    public async Task BlockTest()
    {
      var _sync = new object();
      
      // Batch block combines messages into batches
      var batchBlock = new BatchBlock<int>(10, new GroupingDataflowBlockOptions{Greedy = true});

      Action<int[]> action = async batch =>
      {
        lock (_sync)
        {
          Console.Write("{0}: [", Thread.CurrentThread.ManagedThreadId);
          foreach (var elem in batch)
          {
            Console.Write("{0,4}", elem);
          }
          Console.WriteLine("]");
        }
        await Task.Delay(100);
      };

      // Action blocks process message in batches
      var actionBlock = new ActionBlock<int[]>(action, new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = 2});

      // Create network
      batchBlock.LinkTo(actionBlock);

      // Configure network
// ReSharper disable CSharpWarnings::CS4014
      batchBlock.Completion.ContinueWith(t =>
      {
        if (t.IsFaulted)
        {
          ((IDataflowBlock) actionBlock).Fault(t.Exception);
        }
        else
        {
          actionBlock.Complete();
        }
      });
// ReSharper restore CSharpWarnings::CS4014

      await SendAsync(batchBlock);
      await actionBlock.Completion;
    }

    [Test]
    public void ThrottleTest()
    {
      var net = new FakeProcessingNetwork();
      net.Process().Wait();
    }

    private static async Task SendAsync(ITargetBlock<int> target)
    {
      for (var i = 0; i < 305; i++)
      {
        await target.SendAsync(i);
      }
      target.Complete();
    }
  }
}