using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    public Dictionary<TKey, TValue> Dictionary => dictionary;

    public void OnBeforeSerialize()
    {
        // Rebuild dictionary from lists
        dictionary.Clear();

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            if (!dictionary.ContainsKey(keys[i]))
                dictionary.Add(keys[i], values[i]);
        }
    }

    public void OnAfterDeserialize()
    {
        // Same logic: lists are source of truth
        dictionary.Clear();

        int count = Math.Min(keys.Count, values.Count);
        for (int i = 0; i < count; i++)
        {
            if (!dictionary.ContainsKey(keys[i]))
                dictionary.Add(keys[i], values[i]);
        }
    }
}