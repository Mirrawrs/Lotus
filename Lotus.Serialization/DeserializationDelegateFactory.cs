using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lotus.Serialization.Attributes;

namespace Lotus.Serialization
{
    internal class DeserializationDelegateFactory : DelegateFactoryBase
    {
        public DeserializationDelegateFactory(Type serializerType) : base(
            serializerType,
            typeof(ReaderAttribute),
            typeof(BeforeReadingAttribute),
            typeof(AfterReadingAttribute), 
            method => method.ReturnType)
        {
        }

        public DeserializationDelegate<TOutput> GetDelegate<TOutput>()
            => (DeserializationDelegate<TOutput>) GetDelegate(new DirectiveInfo(typeof(TOutput)));

        public DeserializationDelegate GetWeakTypedDelegate(Type outputType)
            => (DeserializationDelegate) GetWeakTypedDelegate(new DirectiveInfo(outputType));

        protected override Delegate CreateWeakTypedDelegate(DirectiveInfo parameters)
        {
            var deserializationDelegate = Expression.Constant(GetDelegate(parameters));
            var serializer = Expression.Parameter(typeof(object));
            var invoke = Expression.Invoke(deserializationDelegate, serializer);
            var boxedOutput = Expression.Convert(invoke, typeof(object));
            return Expression.Lambda<DeserializationDelegate>(boxedOutput, serializer).Compile();
        }

        private static Delegate CompileDelegate(
            Type outputType,
            Expression body,
            ParameterExpression serializer)
        {
            var delegateType = typeof(DeserializationDelegate<>).MakeGenericType(outputType);
            return Expression.Lambda(delegateType, body, serializer).Compile();
        }

        protected override Delegate CreatePrimitiveDelegate(
            Type outputType,
            MethodInfo directive,
            bool explicitlyConvert)
        {
            var weakTypedSerializer = Expression.Parameter(typeof(object));
            var serializer = Expression.Convert(weakTypedSerializer, SerializerType);
            var call = Expression.Call(serializer, directive);
            var body = explicitlyConvert
                ? (Expression) Expression.Convert(call, outputType)
                : call;
            return CompileDelegate(outputType, body, weakTypedSerializer);
        }

        protected override Delegate CreateComplexDelegate(DirectiveInfo directiveInfo)
        {
            var (outputType, directiveSelector) = directiveInfo;
            var serializer = Expression.Parameter(typeof(object));
            var output = Expression.Variable(outputType);
            var assignNewInstance = Expression.Assign(output, Expression.New(type: outputType));
            var assignProperties = GetSerializableProperties(outputType)
                .Select(property =>
                {
                    var propertyDirectiveSelector = GetDirectiveSelector(property);

                    var propertyDirective = Expression.Constant(GetDelegate(
                        new DirectiveInfo(property.PropertyType, propertyDirectiveSelector)));
                    Expression invokeDirective = Expression.Invoke(propertyDirective, serializer);
                    return Expression.Assign(Expression.Property(output, property), invokeDirective);
                });
            var (preProcessor, postProcessor) = GetProcessorCalls(serializer, output, directiveSelector);
            var body = Expression.Block(
                new[] {output},
                new[] {assignNewInstance, preProcessor}
                    .Concat(assignProperties)
                    .Concat(new[] {postProcessor, output}));
            return CompileDelegate(outputType, body, serializer);
        }
        

        internal delegate TOutput DeserializationDelegate<out TOutput>(object serializer);

        internal delegate object DeserializationDelegate(object serializer);
    }
}