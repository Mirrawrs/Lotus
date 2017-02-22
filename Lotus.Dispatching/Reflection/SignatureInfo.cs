using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lotus.Dispatching.Metadata;

namespace Lotus.Dispatching.Reflection
{
    internal class SignatureInfo : ISignatureInfo
    {
        public SignatureInfo(MethodInfo method)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            var parameters = method.GetParameters().ToList();
            if (parameters.LastOrDefault() is var lastParameter &&
                lastParameter?.ParameterType == typeof(CancellationToken))
            {
                //If the last parameter is a cancellation token, it's treated separatedly from the other ones.
                SupportsCancellation = true;
                parameters.Remove(lastParameter);
            }
            Parameters = parameters.Select(parameter => new ParameterMetadata(parameter)).ToList();
            var returnType = method.ReturnType;
            IsAsync = returnType == typeof(Task) || returnType.GetTypeInfo().IsSubclassOf(typeof(Task));
        }

        public IReadOnlyList<IParameterMetadata> Parameters { get; }
        public MethodInfo Method { get; }
        public bool SupportsCancellation { get; }
        public bool IsAsync { get; }

        public override bool Equals(object obj)
        {
            return obj is SignatureInfo other && Method == other.Method;
        }

        public override int GetHashCode() => Method.GetHashCode();
    }
}