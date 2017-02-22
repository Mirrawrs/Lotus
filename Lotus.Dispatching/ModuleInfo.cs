using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lotus.Dispatching.Metadata;

namespace Lotus.Dispatching
{
    internal class ModuleInfo : IModuleMetadata
    {
        public ModuleInfo(Type type, ConverterDictionary converters)
        {
            var typeInfo = type.GetTypeInfo();
            var canBeCreated = typeInfo.IsClass &&
                               !typeInfo.IsAbstract &&
                               typeInfo.DeclaredConstructors.Any(c => !c.GetParameters().Any());
            if (!canBeCreated) throw new ArgumentException(nameof(type));
            Type = type;
            CommandInfos = CommandInfo.GetCommands(type, converters).ToList();
            ListenerInfos = ListenerInfo.GetListeners(type).ToList();
            (Name, Description) = typeInfo.GetDescriptiveMetadata();
        }

        public List<CommandInfo> CommandInfos { get; }
        public List<ListenerInfo> ListenerInfos { get; }
        public string Name { get; }
        public string Description { get; }
        public Type Type { get; }
        public IReadOnlyCollection<IListenerMetadata> Listeners => ListenerInfos;
        public IReadOnlyCollection<ICommandMetadata> Commands => CommandInfos;
    }
}