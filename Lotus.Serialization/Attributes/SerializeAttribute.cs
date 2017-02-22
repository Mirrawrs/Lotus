using System;

namespace Lotus.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializeAttribute : Attribute
    {
        public SerializeAttribute(int order, int absoluteOrder = 0)
        {
            Order = order;
            AbsoluteOrder = absoluteOrder;
        }

        public int Order { get; }
        public int AbsoluteOrder { get; }
    }
}