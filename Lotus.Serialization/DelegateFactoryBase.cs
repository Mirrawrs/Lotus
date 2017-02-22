using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lotus.Serialization.Attributes;
using DelegatesDictionary = System.Collections.Generic.Dictionary<Lotus.Serialization.DirectiveInfo, System.Delegate>;

namespace Lotus.Serialization
{
    internal abstract class DelegateFactoryBase
    {
        private readonly DelegatesDictionary delegates = new DelegatesDictionary();
        private readonly Dictionary<DirectiveInfo, MethodInfo> directives;
        private readonly Dictionary<DirectiveInfo, MethodInfo> genericDirectives;
        private readonly Dictionary<DirectiveInfo, MethodInfo> genericPostProcessors;
        private readonly Dictionary<DirectiveInfo, MethodInfo> genericPreProcessors;
        private readonly Dictionary<DirectiveInfo, MethodInfo> postProcessors;
        private readonly Dictionary<DirectiveInfo, MethodInfo> preProcessors;
        private readonly DelegatesDictionary weaklyTypedDelegates = new DelegatesDictionary();


        protected DelegateFactoryBase(
            Type serializerType,
            Type directiveAttributeType,
            Type preProcessAttributeType,
            Type postProcessAttributeType,
            Func<MethodInfo, Type> directiveTypeGetter)
        {
            Dictionary<DirectiveInfo, MethodInfo> GetGenericVersion(Dictionary<DirectiveInfo, MethodInfo> source)
                => source
                    .Where(pair => pair.Key.Type.IsConstructedGenericType)
                    .ToDictionary(
                        kvp => new DirectiveInfo(
                            kvp.Key.Type.GetGenericTypeDefinition(),
                            kvp.Key.SelectorAttributeType),
                        kvp => kvp.Value);

            DirectiveInfo GetSingleParameterType(MethodBase method)
                => (method.GetParameters().Single().ParameterType, GetDirectiveSelector(method));

            SerializerType = serializerType;
            var methods = SerializerType.GetRuntimeMethods().ToList();
            directives = methods
                .Where(method => method.IsDefined(directiveAttributeType))
                .ToDictionary(method => new DirectiveInfo(directiveTypeGetter(method), GetDirectiveSelector(method)));
            genericDirectives = GetGenericVersion(directives);
            preProcessors = methods
                .Where(method => method.IsDefined(preProcessAttributeType))
                .ToDictionary(GetSingleParameterType);
            postProcessors = methods
                .Where(method => method.IsDefined(postProcessAttributeType))
                .ToDictionary(GetSingleParameterType);
            genericPreProcessors = GetGenericVersion(preProcessors);
            genericPostProcessors = GetGenericVersion(postProcessors);
        }

        protected Type SerializerType { get; }

        protected static Type GetDirectiveSelector(MemberInfo member)
            => member.GetCustomAttribute<DirectiveSelectorAttribute>()?.GetType();

        private static Delegate GetOrAdd(
            DelegatesDictionary source,
            DirectiveInfo key,
            Func<DirectiveInfo, Delegate> directiveFactory)
            => source.TryGetValue(key, out var directive)
                ? directive
                : source[key] = directiveFactory(key);

        protected Delegate GetDelegate(DirectiveInfo parameters)
            => GetOrAdd(delegates, parameters, CreateDelegate);

        protected Delegate GetWeakTypedDelegate(DirectiveInfo parameters)
            => GetOrAdd(weaklyTypedDelegates, parameters, CreateWeakTypedDelegate);

        private Delegate CreateDelegate(DirectiveInfo parameters)
        {
            var type = parameters.Type;
            return TryGetDirective(parameters, out var directiveInfo)
                ? CreatePrimitiveDelegate(type, directiveInfo.method, directiveInfo.explicitlyConvert)
                : type.GetRuntimeProperties().Any(property => property.IsDefined(typeof(SerializeAttribute)))
                    ? CreateComplexDelegate(parameters)
                    : throw new Exception($"No directive found for type: {type}");
        }

        protected (Expression pre, Expression post) GetProcessorCalls(
            Expression weakTypedSerializer,
            Expression strongTypedValue,
            Type directiveSelector)
        {
            Expression GetProcessorCall(
                IDictionary<DirectiveInfo, MethodInfo> processors,
                IDictionary<DirectiveInfo, MethodInfo> genericProcessors)
            {
                var serializer = Expression.Convert(weakTypedSerializer, SerializerType);
                var calls = new List<Expression>();
                foreach (var type in GetTypeHierarchy(strongTypedValue.Type))
                {
                    if (processors.TryGetValue((type, directiveSelector), out var processor))
                        calls.Add(Expression.Call(serializer, processor, Expression.Convert(strongTypedValue, type)));
                    var generic = GetGenericTypeOrNull(type);
                    if (generic != null && genericProcessors.TryGetValue((generic, directiveSelector), out processor))
                    {
                        processor = processor.MakeGenericMethod(type.GenericTypeArguments);
                        calls.Add(Expression.Call(serializer, processor, Expression.Convert(strongTypedValue, type)));
                    }
                }
                return calls.Any() ? (Expression) Expression.Block(calls) : Expression.Empty();
            }

            return (GetProcessorCall(preProcessors, genericPreProcessors),
                GetProcessorCall(postProcessors, genericPostProcessors));
        }

        protected abstract Delegate CreatePrimitiveDelegate(Type type, MethodInfo directive, bool explicitlyConvert);

        protected abstract Delegate CreateComplexDelegate(DirectiveInfo type);

        protected abstract Delegate CreateWeakTypedDelegate(DirectiveInfo parameters);

        private Type GetGenericTypeOrNull(Type type) => type.IsConstructedGenericType
            ? type.GetGenericTypeDefinition()
            : null;

        private bool TryGetDirective(
            DirectiveInfo parameters,
            out (MethodInfo method, bool explicitlyConvert) callInfo)
        {
            var (type, selector) = parameters;

            //TODO turn into expression body.
            Type GetUnderlyingTypeOrNull()
            {
                var info = type.GetTypeInfo();
                return info.IsEnum ? info.GetEnumUnderlyingType() : null;
            }

            callInfo = default((MethodInfo, bool));
            var foundDirective = directives.TryGetValue(parameters, out callInfo.method);
            var foundGenericDirective = !foundDirective && GetGenericTypeOrNull(type) is Type generic &&
                                        genericDirectives.TryGetValue((generic, selector), out callInfo.method);
            var foundEnumDirective = !foundGenericDirective && GetUnderlyingTypeOrNull() is Type underlying &&
                                     directives.TryGetValue((underlying, selector), out callInfo.method);
            callInfo.explicitlyConvert = foundEnumDirective;
            if (foundGenericDirective) callInfo.method = callInfo.method.MakeGenericMethod(type.GenericTypeArguments);
            return foundDirective || foundGenericDirective || foundEnumDirective;
        }

        protected static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
        {
            var hierarchy = GetTypeHierarchy(type).Reverse().ToList();
            return from property in type.GetRuntimeProperties()
                let attribute = property.GetCustomAttribute<SerializeAttribute>()
                where attribute != null
                orderby attribute?.AbsoluteOrder, hierarchy.IndexOf(property.DeclaringType), attribute?.Order
                select property;
        }

        private static IEnumerable<Type> GetTypeHierarchy(Type type)
        {
            for (var current = type; current != null; current = current.GetTypeInfo().BaseType)
                yield return current;
        }
    }
}