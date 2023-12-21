using System;
using System.Collections.Generic;

namespace Dex.Types
{
    public class ReversibleDictionary<TKey, TValue> where TKey : notnull where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forwardDictionary = new Dictionary<TKey, TValue>();
        private readonly Dictionary<TValue, TKey> _reverseDictionary = new Dictionary<TValue, TKey>();

        public void Add(TKey key, TValue value)
        {
            if (!_forwardDictionary.ContainsKey(key) && !_reverseDictionary.ContainsKey(value))
            {
                _forwardDictionary.Add(key, value);
                _reverseDictionary.Add(value, key);
            }
            else
            {
                throw new ArgumentException($"Key {key} or value {value} already exists in the BiDictionary.");
            }
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return _reverseDictionary.TryGetValue(value, out key!);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _forwardDictionary.TryGetValue(key, out value!);
        }
    }
}