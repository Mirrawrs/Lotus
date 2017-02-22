using System;

namespace Lotus.Dispatching.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(ulong? level = default(ulong?))
        {
            Level = level;
        }

        internal ulong? Level { get; }
    }
}