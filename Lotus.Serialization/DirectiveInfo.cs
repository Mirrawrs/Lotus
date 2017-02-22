using System;

namespace Lotus.Serialization
{
    internal struct DirectiveInfo
    {
        public DirectiveInfo(Type type, Type selectorAttributeType = null)
        {
            Type = type;
            SelectorAttributeType = selectorAttributeType;
        }

        public Type Type { get; }
        public Type SelectorAttributeType { get; }

        public void Deconstruct(out Type type, out Type directiveSelectorAttributeType)
        {
            type = Type;
            directiveSelectorAttributeType = SelectorAttributeType;
        }

        public static implicit operator DirectiveInfo((Type, Type) tuple)
        {
            return new DirectiveInfo(tuple.Item1, tuple.Item2);
        }
    }
}