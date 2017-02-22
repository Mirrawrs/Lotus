using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lotus.Serialization.Attributes;

namespace Lotus.Serialization
{
    /// <summary>
    ///     Creates delegates that serialize objects by calling the appropriate write directives.
    /// </summary>
    internal class SerializationDelegateFactory : DelegateFactoryBase
    {
        public SerializationDelegateFactory(Type serializerType) : base(
            serializerType,
            typeof(WriterAttribute),
            typeof(BeforeWritingAttribute),
            typeof(AfterWritingAttribute), 
            method => method.GetParameters().Single().ParameterType)
        {
        }

        public SerializationDelegate<TInput> GetDelegate<TInput>()
            => (SerializationDelegate<TInput>) GetDelegate(new DirectiveInfo(typeof(TInput)));

        public SerializationDelegate GetWeakTypedDelegate(Type inputType)
            => (SerializationDelegate) GetWeakTypedDelegate(new DirectiveInfo(inputType));

        protected override Delegate CreateWeakTypedDelegate(DirectiveInfo parameters)
        {
            var serializationDelegate = Expression.Constant(GetDelegate(parameters));
            var serializer = Expression.Parameter(typeof(object));
            var weakTypedInput = Expression.Parameter(typeof(object));
            var input = Expression.Convert(weakTypedInput, parameters.Type);
            var invoke = Expression.Invoke(serializationDelegate, serializer, input);
            return Expression.Lambda<SerializationDelegate>(invoke, serializer, weakTypedInput).Compile();
        }

        private static Delegate CompileDelegate(
            ParameterExpression input, 
            Expression body,
            ParameterExpression serializer)
        {
            var delegateType = typeof(SerializationDelegate<>).MakeGenericType(input.Type);
            return Expression.Lambda(delegateType, body, serializer, input).Compile();
        }

        protected override Delegate CreatePrimitiveDelegate(
            Type inputType, 
            MethodInfo directive,
            bool explicitlyConvert)
        {
            var weakTypedSerializer = Expression.Parameter(typeof(object));
            var input = Expression.Parameter(inputType);
            var serializer = Expression.Convert(weakTypedSerializer, SerializerType);
            var convertInput = explicitlyConvert
                ? Expression.Convert(input, directive.GetParameters().Single().ParameterType)
                : (Expression) input;
            var call = Expression.Call(serializer, directive, convertInput);
            return CompileDelegate(input, call, weakTypedSerializer);
        }

        protected override Delegate CreateComplexDelegate(DirectiveInfo parameters)
        {
            var (inputType, directiveSelector) = parameters;
            var serializer = Expression.Parameter(typeof(object));
            var input = Expression.Parameter(inputType);
            var readProperties = GetSerializableProperties(inputType)
                .Select(property =>
                {
                    var propertyDirectiveSelector = GetDirectiveSelector(property);
                    var propertyDirective = Expression.Constant(GetDelegate(
                        new DirectiveInfo(property.PropertyType, propertyDirectiveSelector)));
                    var propertyExpression = Expression.Property(input, property);
                    return (Expression) Expression.Invoke(propertyDirective, serializer, propertyExpression);
                });
            var (preProcessor, postProcessor) = GetProcessorCalls(serializer, input, directiveSelector);
            var body = Expression.Block(new[] {preProcessor}.Concat(readProperties).Concat(new[] {postProcessor}));
            return CompileDelegate(input, body, serializer);
        }
        

        internal delegate void SerializationDelegate<in TInput>(object serializer, TInput input);

        internal delegate void SerializationDelegate(object serializer, object input);
    }
}