using System;

namespace Lotus.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BeforeWritingAttribute : Attribute
    {
    }
}