using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Lotus.Dispatching.Metadata;

namespace Lotus.Dispatching
{
    public class Dispatcher : IDisposable
    {
        private readonly ILookup<string, (CommandInfo command, object module)> commandsByName;
        private readonly DispatcherConfiguration configuration;
        private readonly ILookup<Type, (ListenerInfo, object)> listenersByParameterType;

        private readonly IDictionary<Type, List<(ListenerInfo, object)>> transientListenersByParameterType =
            new Dictionary<Type, List<(ListenerInfo, object)>>();

        public Dispatcher(DispatcherConfiguration configuration = default(DispatcherConfiguration))
        {
            this.configuration = configuration ?? new DispatcherConfiguration();

            ModuleInfo GetModuleInfo(Type type) => Cache.Get(
                (type, this.configuration.ConverterType),
                key => new ModuleInfo(key.Item1, ConverterDictionary.Get(key.Item2)));

            var moduleInfos = this.configuration.ModuleTypes
                .Select(GetModuleInfo)
                .ToList();
            ModulesMetadata = moduleInfos;
            var modules = moduleInfos
                .Select(info => (instance: Activator.CreateInstance(info.Type), info: info))
                .ToList();
            Modules = modules.Select(module => module.instance);
            commandsByName = modules
                .SelectMany(m => m.info.CommandInfos, (m, command) => (command: command, instance: m.instance))
                .ToLookup(context => context.command.Name, this.configuration.CommandsComparer);
            listenersByParameterType = modules
                .SelectMany(m => m.info.ListenerInfos, (m, listener) => (listener: listener, instance: m.instance))
                .ToLookup(context => context.listener.Signature.Parameters.Single().Info.ParameterType);
        }

        public IReadOnlyCollection<IModuleMetadata> ModulesMetadata { get; }
        public IEnumerable<object> Modules { get; }

        public void Dispose()
        {
            foreach (var module in Modules.OfType<IDisposable>()) module.Dispose();
        }

        public Task Execute(
            string commandName,
            object[] arguments,
            ulong level,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var (command, module) = commandsByName[commandName]
                .Single(c => c.command.Signature.Parameters.Count == arguments.Length);

            bool IsLevelSufficient()
            {
                var commandLevel = command.Level;
                var levelComparisonType = configuration.LevelComparisonType;
                switch (levelComparisonType)
                {
                    case LevelComparisonType.BitwiseAnd:
                        return (level & commandLevel) == commandLevel;
                    case LevelComparisonType.GreaterOrEqual:
                        return level >= commandLevel;
                    default:
                        return false;
                }
            }

            if (!IsLevelSufficient()) throw new ArgumentException(nameof(level));
            return command.Execute(module, arguments, cancellationToken);
        }

        public Task Notify(object value, CancellationToken cancellationToken = default(CancellationToken))
        {
            IEnumerable<Type> GetTypeHierarchy(Type type)
            {
                for (var current = type; current != null; current = current.GetTypeInfo().BaseType)
                    yield return current;
            }

            List<(ListenerInfo listener, object module)> GetListeners(Type parameterType)
            {
                transientListenersByParameterType.TryGetValue(parameterType, out var transientListeners);
                return listenersByParameterType[parameterType]
                    .Concat(transientListeners ?? Enumerable.Empty<(ListenerInfo, object)>())
                    .ToList();
            }

            var argumentType = value.GetType();
            var contexts = configuration.ExactTypeOnlyNotifications
                ? GetListeners(argumentType)
                : GetTypeHierarchy(argumentType).SelectMany(GetListeners);
            var notifications = contexts
                .Select(context => context.listener.Notify(context.module, value, cancellationToken));
            return Task.WhenAll(notifications);
        }

        public Task<TValue> Next<TValue>(
            Predicate<TValue> predicate,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<TValue>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var key = typeof(TValue);
            //TODO turn to if block
            var listeners = transientListenersByParameterType.TryGetValue(key, out var temp)
                ? temp
                : transientListenersByParameterType[key] = new List<(ListenerInfo, object)>();
            Action<TValue> action = value =>
            {
                if (predicate == null || predicate(value)) tcs.TrySetResult(value);
            };
            var context = (new ListenerInfo(action.GetMethodInfo()), action.Target);
            listeners.Add(context);
            var task = tcs.Task;
            task.ContinueWith(_ => listeners.Remove(context), TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        public Task<TValue> Next<TValue>(CancellationToken cancellationToken = default(CancellationToken))
            => Next<TValue>(null, cancellationToken);

        public TModule Get<TModule>() => Modules.OfType<TModule>().SingleOrDefault();
    }
}