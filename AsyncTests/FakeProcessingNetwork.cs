using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace AsyncTests
{
  public class FakeProcessingNetwork
  {
    private readonly Random _random;
    private readonly object _rndSync = new object();
    private readonly ActionBlock<int[]> _actionBlock;
    private readonly BatchBlock<int> _batchBlock;
    private long _bufferCapacity = 0;

    public FakeProcessingNetwork()
    {
      _random = new Random((int)DateTime.Now.Ticks);

      var actionOptions = new ExecutionDataflowBlockOptions
      {
        MaxDegreeOfParallelism = -1,
        BoundedCapacity = -1
      };
      _actionBlock = new ActionBlock<int[]>((Action<int[]>) FakeAction, actionOptions);
      _actionBlock.Completion.ContinueWith(t => Console.WriteLine(t.IsFaulted ? "\r\nProcessor faulted" : "\r\nProcessor completed"));


      var batchOptions = new GroupingDataflowBlockOptions
      {
        Greedy = true,
        BoundedCapacity = -1
      };
      _batchBlock = new BatchBlock<int>(10, batchOptions);

      _batchBlock.Completion.ContinueWith(t =>
      {
        if (t.IsFaulted)
        {
          ((IDataflowBlock)_actionBlock).Fault(t.Exception);
        }
        else
        {
          Console.WriteLine("\r\nInput completed");
          _actionBlock.Complete();
        }        
      });

      _batchBlock.LinkTo(_actionBlock);
    }

    public async Task Process()
    {
      await SendData();
      await _actionBlock.Completion;
    }

    private async Task SendData()
    {
      for (var i = 1; i <= 300; i++)
      {
        var sw = Stopwatch.StartNew();
        var success = await _batchBlock.SendAsync(i);
        Console.Write("{0} ", sw.ElapsedTicks);
        if (!success)
        {
          Console.WriteLine("Not accepted");
        }
        Interlocked.Exchange(ref _bufferCapacity, _batchBlock.OutputCount);
      }

      _batchBlock.Complete();
    }

    private void FakeAction(int[] data)
    {
      const int minDelay = 100;
      const int maxDelay = 500;

      // Console.WriteLine("{1,4}: [{0}]", string.Join(",", data.Select(i => i.ToString("").PadLeft(4))), Thread.CurrentThread.ManagedThreadId);
      // Console.WriteLine(_batchBlock.OutputCount);
//      Console.Write("[{0}]", Thread.CurrentThread.ManagedThreadId);
//      Task.Delay(GetNextRandom(minDelay, maxDelay)).Wait();
      Task.Delay(1000).Wait();
    }

    private int GetNextRandom(int min, int max)
    {
      lock (_rndSync)
      {
        return _random.Next(min, max);
      }
    }
  }
}