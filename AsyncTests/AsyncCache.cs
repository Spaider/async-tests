using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AsyncTests
{

  /// <summary>
  /// Simple asynchronous cache that prevents task execution if result for given input parameter
  /// is already known
  /// </summary>
  /// <typeparam name="TKey">Task input type parameter</typeparam>
  /// <typeparam name="TValue">Output type parameter</typeparam>
  public class AsyncCache<TKey, TValue>
  {
    private readonly Func<TKey, Task<TValue>> _valueFactory;
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _map;

    public AsyncCache(Func<TKey, Task<TValue>> valueFactory)
    {
      if (valueFactory == null) throw new ArgumentNullException("valueFactory");

      _valueFactory = valueFactory;
      _map = new ConcurrentDictionary<TKey, Lazy<Task<TValue>>>();
    }

    public Task<TValue> this[TKey key]
    {
      get
      {
        // ReSharper disable CompareNonConstrainedGenericWithNull
        if (!typeof(TKey).IsValueType && key == null) 
          throw new ArgumentNullException("key");
        // ReSharper restore CompareNonConstrainedGenericWithNull

        return _map.GetOrAdd(
          key, 
          toAdd => new Lazy<Task<TValue>>(() => _valueFactory(toAdd))).Value;
      }
    }
  }
}