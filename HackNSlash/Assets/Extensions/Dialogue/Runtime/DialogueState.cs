using System;
using System.Collections.Generic;
using UnityEngine;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Type-safe blackboard for storing dialogue state without boxing.
    /// Maintains separate dictionaries for int, bool, float, and string types.
    /// </summary>
    [Serializable]
    public sealed class DialogueState
    {
        [SerializeField] private Dictionary<int, int> _intValues = new();
        [SerializeField] private Dictionary<int, bool> _boolValues = new();
        [SerializeField] private Dictionary<int, float> _floatValues = new();
        [SerializeField] private Dictionary<int, string> _stringValues = new();

        public int Get(Data.StateKey<int> key)
        {
            return _intValues.TryGetValue(key.Id, out var value) ? value : 0;
        }

        public void Set(Data.StateKey<int> key, int value)
        {
            _intValues[key.Id] = value;
        }

        public bool Get(Data.StateKey<bool> key)
        {
            return _boolValues.TryGetValue(key.Id, out var value) && value;
        }

        public void Set(Data.StateKey<bool> key, bool value)
        {
            _boolValues[key.Id] = value;
        }

        public float Get(Data.StateKey<float> key)
        {
            return _floatValues.TryGetValue(key.Id, out var value) ? value : 0f;
        }

        public void Set(Data.StateKey<float> key, float value)
        {
            _floatValues[key.Id] = value;
        }

        public string Get(Data.StateKey<string> key)
        {
            return _stringValues.TryGetValue(key.Id, out var value) ? value : "";
        }

        public void Set(Data.StateKey<string> key, string value)
        {
            _stringValues[key.Id] = value;
        }

        public void Reset()
        {
            _intValues.Clear();
            _boolValues.Clear();
            _floatValues.Clear();
            _stringValues.Clear();
        }
    }
}

