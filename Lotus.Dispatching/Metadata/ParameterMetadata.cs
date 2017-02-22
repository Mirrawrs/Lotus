using System.Reflection;
using Lotus.Dispatching.Attributes;

namespace Lotus.Dispatching.Metadata
{
    internal class ParameterMetadata : IParameterMetadata
    {
        public ParameterMetadata(ParameterInfo info)
        {
            Info = info;
            var nameAttribute = Info.GetCustomAttribute<ComponentNameAttribute>();
            var descriptionAttribute = Info.GetCustomAttribute<ComponentDescriptionAttribute>();
            Name = nameAttribute?.Name ?? Info.Name;
            Description = descriptionAttribute?.Description;
        }

        public ParameterInfo Info { get; }
        public string Name { get; }
        public string Description { get; }

        public override bool Equals(object obj)
        {
            return obj is ParameterMetadata other && Info == other.Info;
        }

        public override int GetHashCode() => Info.GetHashCode();
    }
}