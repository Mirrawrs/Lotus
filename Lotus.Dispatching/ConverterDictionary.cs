using System;
using System.Linq;
using System.Reflection;
using Lotus.Dispatching.Attributes;

namespace Lotus.Dispatching
{
    internal class ConverterDictionary
    {
        private readonly ILookup<(Type, Type), MethodInfo> converters;

        private ConverterDictionary(Type converterType)
        {
            converters = converterType.GetRuntimeMethods()
                .Where(method => method.IsDefined(typeof(ConverterAttribute)))
                .ToLookup(GetIOTypes);
        }

        public static ConverterDictionary Empty { get; } = new ConverterDictionary(typeof(void));
        public MethodInfo this[Type input, Type output] => converters[(input, output)].FirstOrDefault();

        public static ConverterDictionary Get(Type converterType)
        {
            return converterType == null ? Empty : Cache.Get(converterType, type => new ConverterDictionary(type));
        }

        private static (Type, Type) GetIOTypes(MethodInfo converterMethod)
        {
            var inputType = converterMethod.GetParameters().Single().ParameterType;
            return (inputType, converterMethod.ReturnType);
        }
    }
}