using System;
using UnityEngine;

namespace Extensions.Dialogue.Data
{
    /// <summary>
    /// Strongly-typed identifier for dialogue nodes. Prevents bugs from using strings.
    /// </summary>
    [Serializable]
    public readonly struct NodeId : IEquatable<NodeId>
    {
        [SerializeField] private readonly int _value;

        public int Value => _value;

        public NodeId(int value)
        {
            _value = value;
        }

        public static NodeId Invalid => new NodeId(-1);

        public bool Equals(NodeId other) => _value == other._value;
        public override bool Equals(object obj) => obj is NodeId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();

        public static bool operator ==(NodeId left, NodeId right) => left.Equals(right);
        public static bool operator !=(NodeId left, NodeId right) => !left.Equals(right);
    }

    /// <summary>
    /// Strongly-typed identifier for speakers (characters).
    /// </summary>
    [Serializable]
    public readonly struct SpeakerId : IEquatable<SpeakerId>
    {
        [SerializeField] private readonly int _value;

        public int Value => _value;

        public SpeakerId(int value)
        {
            _value = value;
        }

        public static SpeakerId None => new SpeakerId(-1);

        public bool Equals(SpeakerId other) => _value == other._value;
        public override bool Equals(object obj) => obj is SpeakerId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();

        public static bool operator ==(SpeakerId left, SpeakerId right) => left.Equals(right);
        public static bool operator !=(SpeakerId left, SpeakerId right) => !left.Equals(right);
    }

    /// <summary>
    /// Strongly-typed identifier for dialogue text entries (for localization).
    /// </summary>
    [Serializable]
    public readonly struct TextKey : IEquatable<TextKey>
    {
        [SerializeField] private readonly int _value;

        public int Value => _value;

        public TextKey(int value)
        {
            _value = value;
        }

        public static TextKey None => new TextKey(-1);

        public bool Equals(TextKey other) => _value == other._value;
        public override bool Equals(object obj) => obj is TextKey other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();

        public static bool operator ==(TextKey left, TextKey right) => left.Equals(right);
        public static bool operator !=(TextKey left, TextKey right) => !left.Equals(right);
    }

    /// <summary>
    /// Strongly-typed identifier for dialogue text styles.
    /// </summary>
    [Serializable]
    public readonly struct StyleId : IEquatable<StyleId>
    {
        [SerializeField] private readonly int _value;

        public int Value => _value;

        public StyleId(int value)
        {
            _value = value;
        }

        public static StyleId Default => new StyleId(0);

        public bool Equals(StyleId other) => _value == other._value;
        public override bool Equals(object obj) => obj is StyleId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public override string ToString() => _value.ToString();

        public static bool operator ==(StyleId left, StyleId right) => left.Equals(right);
        public static bool operator !=(StyleId left, StyleId right) => !left.Equals(right);
    }

    /// <summary>
    /// Generic strongly-typed state key for dialogue state management.
    /// </summary>
    [Serializable]
    public readonly struct StateKey<T> : IEquatable<StateKey<T>>
    {
        [SerializeField] private readonly int _id;

        public int Id => _id;

        public StateKey(int id)
        {
            _id = id;
        }

        public bool Equals(StateKey<T> other) => _id == other._id;
        public override bool Equals(object obj) => obj is StateKey<T> other && Equals(other);
        public override int GetHashCode() => _id.GetHashCode();
        public override string ToString() => _id.ToString();

        public static bool operator ==(StateKey<T> left, StateKey<T> right) => left.Equals(right);
        public static bool operator !=(StateKey<T> left, StateKey<T> right) => !left.Equals(right);
    }
}

