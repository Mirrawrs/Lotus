using System;
using System.Collections.Generic;

namespace Lotus.Dispatching
{
    public class DispatcherConfiguration
    {
        public IEqualityComparer<string> CommandsComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
        public Type ConverterType { get; set; }
        public bool ExactTypeOnlyNotifications { get; set; }
        public LevelComparisonType LevelComparisonType { get; set; }
        public ISet<Type> ModuleTypes { get; set; } = new HashSet<Type>();
    }
}