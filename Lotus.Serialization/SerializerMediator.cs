using System;
using System.Collections.Concurrent;
using Ser = Lotus.Serialization.SerializationDelegateFactory;
using Des = Lotus.Serialization.DeserializationDelegateFactory;

namespace Lotus.Serialization
{
    public class SerializerMediator
    {
        private static readonly ConcurrentDictionary<Type, (Ser, Des)> CachedMappers = new ConcurrentDictionary<Type, (Ser, Des)>();

        private readonly (Ser s, Des d) mappers;
        private readonly object serializer;

        public SerializerMediator(object serializer)
        {
            this.serializer = serializer;
            var serializerType = this.serializer.GetType();
            if (!CachedMappers.TryGetValue(serializerType, out mappers))
            {
                mappers = (new Ser(serializerType), new Des(serializerType));
                CachedMappers[serializerType] = mappers;
            }
        }

        public void Write<TInput>(TInput input) => GetOpenWriteFor<TInput>()(serializer, input);
        public void Write(Type type, object input) => GetOpenWriteFor(type)(serializer, input);
        public TOutput Read<TOutput>() => GetOpenReadFor<TOutput>()(serializer);
        public object Read(Type type) => GetOpenReadFor(type)(serializer);

        public WriteDelegate<TInput> GetWriteFor<TInput>()
        {
            var write = GetOpenWriteFor<TInput>();
            return input => write(serializer, input);
        }

        public WriteDelegate GetWriteFor(Type inputType)
        {
            var write = GetOpenWriteFor(inputType);
            return input => write(serializer, input);
        }

        public ReadDelegate<TOutput> GetReadFor<TOutput>()
        {
            var read = GetOpenReadFor<TOutput>();
            return () => read(serializer);
        }

        public ReadDelegate GetReadFor(Type outputType)
        {
            var read = GetOpenReadFor(outputType);
            return () => read(serializer);
        }

        private Ser.SerializationDelegate<TInput> GetOpenWriteFor<TInput>() => mappers.s.GetDelegate<TInput>();

        private Ser.SerializationDelegate GetOpenWriteFor(Type inputType) => mappers.s.GetWeakTypedDelegate(inputType);

        private Des.DeserializationDelegate<TOutput> GetOpenReadFor<TOutput>() => mappers.d.GetDelegate<TOutput>();

        private Des.DeserializationDelegate GetOpenReadFor(Type outputType) => mappers.d.GetWeakTypedDelegate(outputType);
    }
}