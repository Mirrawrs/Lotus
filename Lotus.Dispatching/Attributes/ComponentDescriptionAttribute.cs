using System;

namespace Lotus.Dispatching.Attributes
{
    public class ComponentDescriptionAttribute : Attribute
    {
        public ComponentDescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; set; }
    }
}