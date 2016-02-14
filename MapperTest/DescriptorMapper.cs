using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace MapperTest
{
    public static class DescriptorMapper
    {
        public static TDst Map<TSrc, TDst>(TSrc source)
            where TSrc : class
            where TDst : class, new()
        {
            return Map(source, new TDst());
        }
        public static TDst Map<TSrc, TDst>(TSrc source, TDst destination)
            where TSrc : class
            where TDst : class
        {
            var srcProperties = GetCachedProperties(source.GetType());
            var destProperties = GetCachedProperties(destination.GetType()).OfType<PropertyDescriptor>();
            foreach (var prop in destProperties)
            {
                var srcProp =
                    srcProperties.OfType<PropertyDescriptor>()
                                 .FirstOrDefault(p => p.Name == prop.Name && (prop.PropertyType.IsAssignableFrom(p.PropertyType) || prop.PropertyType.IsEnum));
                if (srcProp == null)
                    continue;

                var val = srcProp.GetValue(source);

                if (prop.PropertyType.IsValueType && val == null)
                {
                    prop.SetValue(destination, TypeDescriptor.GetConverter(prop.PropertyType).CreateInstance(null));
                }
                else
                {
                    prop.SetValue(destination, val);
                }
            }

            return destination;
        }

        private static readonly ConcurrentDictionary<Type, Lazy<PropertyDescriptorCollection>> _cachedProperties = new ConcurrentDictionary<Type, Lazy<PropertyDescriptorCollection>>();
        public static PropertyDescriptorCollection GetCachedProperties(Type type)
        {
            var lazy = new Lazy<PropertyDescriptorCollection>(() => TypeDescriptor.GetProperties(type));
            var cacheItem = _cachedProperties.GetOrAdd(type, lazy);

            return (cacheItem ?? lazy).Value;
        }
    }
}
