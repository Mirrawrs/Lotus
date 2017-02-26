using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lotus.Dispatching.Attributes;
using Lotus.Dispatching.Metadata;
using Lotus.Dispatching.Reflection;

namespace Lotus.Dispatching
{
    internal class CommandInfo : ICommandMetadata
    {
        private readonly ConverterDictionary converters;

        private CommandInfo(MethodInfo method, ConverterDictionary converters, ulong level)
        {
            this.converters = converters;
            (Name, Description) = method.GetDescriptiveMetadata();
            Signature = new SignatureInfo(method);
            Level = level;
        }

        public string Name { get; }
        public string Description { get; }
        public ISignatureInfo Signature { get; }
        public ulong Level { get; }

        public static IEnumerable<CommandInfo> GetCommands(Type type, ConverterDictionary converters)
            => from method in type.GetRuntimeMethods()
                let attribute = method.GetCustomAttribute<CommandAttribute>()
                where attribute != null
                select new CommandInfo(method, converters, attribute.Level);

        internal Task Execute(object module, object[] arguments, CancellationToken cancellationToken)
        {
            return Signature.Method.Invoke(module, arguments, cancellationToken, converters);
        }
    }
}