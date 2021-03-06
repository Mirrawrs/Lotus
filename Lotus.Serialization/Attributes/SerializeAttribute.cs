using System;
using System.Runtime.CompilerServices;

namespace Lotus.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializeAttribute : Attribute
    {
        public SerializeAttribute([CallerLineNumber]int order = 0, int absoluteOrder = 0)
        {
            Order = order;
            AbsoluteOrder = absoluteOrder;
        }

        public int Order { get; }
        public int AbsoluteOrder { get; }
    }
}