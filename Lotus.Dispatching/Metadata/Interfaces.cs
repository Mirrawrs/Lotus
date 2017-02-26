using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lotus.Dispatching.Metadata
{
    public interface IComponentMetadata
    {
        string Name { get; }
        string Description { get; }
    }

    public interface IParameterMetadata : IComponentMetadata
    {
        ParameterInfo Info { get; }
    }

    public interface ICommandMetadata : IListenerMetadata
    {
        ulong Level { get; }
    }

    public interface ISignatureInfo
    {
        MethodInfo Method { get; }
        bool SupportsCancellation { get; }
        bool IsAsync { get; }
        IReadOnlyList<IParameterMetadata> Parameters { get; }
    }

    public interface IListenerMetadata : IComponentMetadata
    {
        ISignatureInfo Signature { get; }
    }

    public interface IModuleMetadata : IComponentMetadata
    {
        Type Type { get; }
        IReadOnlyCollection<IListenerMetadata> Listeners { get; }
        IReadOnlyCollection<ICommandMetadata> Commands { get; }
    }
}