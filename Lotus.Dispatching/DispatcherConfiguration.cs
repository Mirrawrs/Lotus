using System;
using System.Collections.Generic;
using System.Linq;

namespace Lotus.Dispatching
{
    public class DispatcherConfiguration
    {
        public IEqualityComparer<string> CommandsComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
        public Type ConverterType { get; set; }
        public bool ExactTypeOnlyNotifications { get; set; }
        public LevelComparisonType LevelComparisonType { get; set; }
        public IEnumerable<Type> ModuleTypes { get; set; } = Enumerable.Empty<Type>();
    }
}