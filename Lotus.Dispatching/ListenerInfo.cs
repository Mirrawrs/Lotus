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
    internal class ListenerInfo : IListenerMetadata
    {
        public ListenerInfo(MethodInfo method)
        {
            (Name, Description) = method.GetDescriptiveMetadata();
            Signature = new SignatureInfo(method);
        }

        public string Name { get; }
        public string Description { get; }
        public ISignatureInfo Signature { get; }

        public static IEnumerable<ListenerInfo> GetListeners(Type type)
            => type.GetRuntimeMethods()
                .Where(method => method.IsDefined(typeof(ListenerAttribute)))
                .Select(method => new ListenerInfo(method));

        internal Task Notify(object module, object value, CancellationToken cancellationToken)
        {
            return Signature.Method.Invoke(module, new[] {value}, cancellationToken, ConverterDictionary.Empty);
        }
    }
}