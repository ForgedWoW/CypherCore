using System.Collections.Generic;

namespace Framework.Collections
{
    public class LoopSafeDoubleDictionary<TKey1, TKey2, TVal> : Dictionary<TKey1, Dictionary<TKey2, TVal>>
    {
        private readonly Dictionary<TKey1, List<TKey2>> _removeCache = new();

        public void QueueRemove(TKey1 key1, TKey2 key2)
        {
            _removeCache.Add(key1, key2);
        }

        public void ExecuteRemove()
        {
            foreach (var kvp in _removeCache)
                foreach (var val in kvp.Value)
                    this.Remove(kvp.Key, val);

            _removeCache.Clear();
        }
    }
}
