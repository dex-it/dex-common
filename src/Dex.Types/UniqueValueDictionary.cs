﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dex.Types
{
    // Allow to use only unique Key and unique Value
    // Allow fast search over Key or Value 
    // All methods are not thread safety
    public sealed class UniqueValueDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : notnull where TValue : notnull
    {
        private readonly Dictionary<TKey, TValue> _forwardDictionary = new Dictionary<TKey, TValue>();
        private readonly Dictionary<TValue, TKey> _reverseDictionary = new Dictionary<TValue, TKey>();

        public TValue this[TKey key]
        {
            get => _forwardDictionary[key];
            set
            {
                if (_forwardDictionary.TryGetValue(key, out var existingValue))
                {
                    if (_reverseDictionary.ContainsKey(value))
                    {
                        throw new ArgumentException(
                            $"Duplicate Value [{value}] in the {nameof(UniqueValueDictionary<TKey, TValue>)}.");
                    }

                    _forwardDictionary[key] = value;
                    _reverseDictionary.Remove(existingValue);
                    _reverseDictionary[value] = key;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public ICollection<TKey> Keys => _forwardDictionary.Keys;

        public ICollection<TValue> Values => _forwardDictionary.Values;

        public int Count => _forwardDictionary.Count;

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            if (_forwardDictionary.ContainsKey(key))
            {
                throw new ArgumentException(
                    $"Duplicate Key [{key}] in the {nameof(UniqueValueDictionary<TKey, TValue>)}.");
            }

            if (_reverseDictionary.ContainsKey(value))
            {
                throw new ArgumentException(
                    $"Duplicate Value [{value}] in the {nameof(UniqueValueDictionary<TKey, TValue>)}.");
            }

            _forwardDictionary.Add(key, value);
            _reverseDictionary.Add(value, key);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _forwardDictionary.Clear();
            _reverseDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _forwardDictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return _forwardDictionary.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return _reverseDictionary.ContainsKey(value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)_forwardDictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(TKey key)
        {
            if (!_forwardDictionary.Remove(key, out var value))
            {
                return false;
            }

            _reverseDictionary.Remove(value);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _forwardDictionary.TryGetValue(key, out value!);
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return _reverseDictionary.TryGetValue(value, out key!);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _forwardDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}