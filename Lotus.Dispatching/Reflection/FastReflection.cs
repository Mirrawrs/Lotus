using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Lotus.Dispatching.Reflection
{
    internal static class FastReflection
    {
        private static readonly IDictionary<(MethodInfo, ConverterDictionary, Type[]), AsyncAction> CachedProxies =
            new Dictionary<(MethodInfo, ConverterDictionary, Type[]), AsyncAction>(
                StructuralComparer<(MethodInfo, ConverterDictionary, Type[])>.Default);

        public static Task Invoke(
            this MethodInfo method,
            object instance,
            object[] arguments,
            CancellationToken token,
            ConverterDictionary converters)
        {
            var argumentTypes = arguments.Select(argument => argument.GetType()).ToArray();
            var key = (method, converters, argumentTypes);
            if (!CachedProxies.TryGetValue(key, out var proxy))
                CachedProxies[key] = proxy = CreateProxy(method, converters, argumentTypes);
            return proxy(instance, arguments, token);
        }

        private static AsyncAction CreateProxy(
            MethodInfo method,
            ConverterDictionary converters,
            Type[] argumentTypes)
        {
            var signature = new SignatureInfo(method);
            AssertMatchingArities(signature, argumentTypes);
            var boxedInstance = Expression.Parameter(typeof(object));
            var boxedArguments = Expression.Parameter(typeof(object[]));
            var cancellationToken = Expression.Parameter(typeof(CancellationToken));
            var instance = UnboxInstance(boxedInstance, method);
            var arguments = UnboxAndConvertArguments(boxedArguments, cancellationToken, signature, converters,
                argumentTypes);
            var methodCall = Expression.Call(instance, method, arguments);
            var body = signature.IsAsync
                ? (Expression) methodCall
                : Expression.Block(methodCall, Expression.Constant(Task.CompletedTask));
            var parameters = new[] {boxedInstance, boxedArguments, cancellationToken};
            var lambda = Expression.Lambda<AsyncAction>(body, parameters);
            return lambda.Compile();
        }

        private static void AssertMatchingArities(SignatureInfo signature, Type[] argumentTypes)
        {
            var parametersCount = signature.Parameters.Count;
            var argumentsCount = argumentTypes.Length;
            if (parametersCount == argumentsCount) return;
            var message = "The number of supplied arguments must match " +
                          $"the signature of method '{signature.Method}'.";
            throw new InvalidOperationException(message);
        }

        private static UnaryExpression UnboxInstance(Expression boxedInstance, MethodInfo method)
        {
            var instanceType = method.DeclaringType;
            var unboxedInstance = !method.IsStatic && instanceType != null
                ? Expression.TypeAs(boxedInstance, instanceType)
                : null;
            return unboxedInstance;
        }

        private static IEnumerable<Expression> UnboxAndConvertArguments(
            ParameterExpression boxedArguments,
            ParameterExpression cancellationToken,
            SignatureInfo signature,
            ConverterDictionary converters,
            Type[] argumentTypes)
        {
            var parameters = signature.Parameters;
            for (var i = 0; i < parameters.Count; i++)
            {
                var boxedArgument = Expression.ArrayIndex(boxedArguments, Expression.Constant(i));
                var argumentType = argumentTypes[i];
                var parameterType = parameters[i].Info.ParameterType;
                var argument = Expression.Convert(boxedArgument, argumentType);
                yield return argumentType == parameterType
                    ? argument
                    : Expression.Convert(argument, parameterType, converters[argument.Type, parameterType]);
            }
            if (signature.SupportsCancellation)
                yield return cancellationToken;
        }

        private class StructuralComparer<T> : IEqualityComparer<T>
        {
            public static StructuralComparer<T> Default { get; } = new StructuralComparer<T>();

            public bool Equals(T x, T y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
            }
        }

        private delegate Task AsyncAction(
            object instance,
            object[] arguments,
            CancellationToken cancellationToken);
    }
}