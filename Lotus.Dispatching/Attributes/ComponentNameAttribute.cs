using System;

namespace Lotus.Dispatching.Attributes
{
    public class ComponentNameAttribute : Attribute
    {
        public ComponentNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}