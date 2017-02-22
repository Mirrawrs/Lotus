using System.Reflection;
using Lotus.Dispatching.Attributes;

namespace Lotus.Dispatching.Metadata
{
    internal static class MetadataExtensionMethods
    {
        public static (string name, string description) GetDescriptiveMetadata(this MemberInfo memberInfo)
        {
            var nameAttribute = memberInfo.GetCustomAttribute<ComponentNameAttribute>();
            var descriptionAttribute = memberInfo.GetCustomAttribute<ComponentDescriptionAttribute>();
            return (nameAttribute?.Name ?? memberInfo.Name, descriptionAttribute?.Description);
        }
    }
}