using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DailyRoutines.Common.Module.Compilers;

internal static class ModuleSerializationCache
{
    public static int RemoveAssemblies
    (
        IReadOnlySet<Assembly> assemblies
    )
    {
        if (assemblies.Count == 0) return 0;

        var jsonAssembly = typeof(JsonConvert).Assembly;
        var removed      = 0;
        (string TypeName, string FieldName)[] stores =
        [
            ("Newtonsoft.Json.Utilities.EnumUtils", "ValuesAndNamesPerEnum"),
            ("Newtonsoft.Json.Converters.DiscriminatedUnionConverter", "UnionCache"),
            ("Newtonsoft.Json.Converters.DiscriminatedUnionConverter", "UnionTypeLookupCache"),
            ("Newtonsoft.Json.Converters.KeyValuePairConverter", "ReflectionObjectPerType"),
            ("Newtonsoft.Json.Utilities.ConvertUtils+CastConverters", "Instance"),
            ("Newtonsoft.Json.Serialization.JsonTypeReflector+CreatorCache", "Instance"),
            ("Newtonsoft.Json.Serialization.JsonTypeReflector+AssociatedMetadataTypesCache", "Instance")
        ];

        foreach (var (typeName, fieldName) in stores)
        {
            var type  = jsonAssembly.GetType(typeName, true);
            var store = type.GetField(fieldName, STATIC_FLAGS).GetValue(null);
            removed += RemoveEntries(GetStore(store), assemblies);
        }

        Type[] attributeTypes =
        [
            typeof(JsonContainerAttribute),
            typeof(JsonConverterAttribute),
            typeof(JsonObjectAttribute),
            typeof(DataContractAttribute),
            typeof(DataMemberAttribute)
        ];

        var attributeGetter = jsonAssembly.GetType("Newtonsoft.Json.Serialization.CachedAttributeGetter`1", true);

        foreach (var attributeType in attributeTypes)
        {
            var type  = attributeGetter.MakeGenericType(attributeType);
            var store = type.GetField("TypeAttributeCache", STATIC_FLAGS).GetValue(null);
            removed += RemoveEntries(GetStore(store), assemblies);
        }

        var resolver      = typeof(DefaultContractResolver).GetField("_instance",      STATIC_FLAGS).GetValue(null);
        var contractStore = typeof(DefaultContractResolver).GetField("_contractCache", INSTANCE_FLAGS).GetValue(resolver);
        removed += RemoveEntries(GetStore(contractStore), assemblies);

        var binder    = typeof(DefaultSerializationBinder).GetField("Instance",   STATIC_FLAGS).GetValue(null);
        var typeStore = typeof(DefaultSerializationBinder).GetField("_typeCache", INSTANCE_FLAGS).GetValue(binder);
        removed += RemoveEntries(GetStore(typeStore), assemblies);

        var camelCaseResolver = typeof(CamelCasePropertyNamesContractResolver);
        var cacheLock         = camelCaseResolver.GetField("TypeContractCacheLock", STATIC_FLAGS).GetValue(null);
        var cacheField        = camelCaseResolver.GetField("_contractCache",        STATIC_FLAGS);

        lock (cacheLock)
        {
            if (cacheField.GetValue(null) is IDictionary cache)
            {
                var replacement = (IDictionary)Activator.CreateInstance(cache.GetType(), cache);
                var count       = RemoveEntries(replacement, assemblies);
                if (count > 0)
                    cacheField.SetValue(null, replacement);

                removed += count;
            }
        }

        var descriptor         = typeof(TypeDescriptor);
        var reflectionProvider = descriptor.Assembly.GetType("System.ComponentModel.ReflectTypeDescriptionProvider", true);
        var descriptorLock     = descriptor.GetField("s_commonSyncObject", STATIC_FLAGS).GetValue(null);

        lock (descriptorLock)
        {
            var             providerTable        = (IDictionary)descriptor.GetField("s_providerTable",              STATIC_FLAGS).GetValue(null);
            var             providerTypeTable    = (IDictionary)descriptor.GetField("s_providerTypeTable",          STATIC_FLAGS).GetValue(null);
            var             initializedProviders = (IDictionary)descriptor.GetField("s_defaultProviderInitialized", STATIC_FLAGS).GetValue(null);
            var             nodeType             = descriptor.GetNestedType("TypeDescriptionNode", BindingFlags.NonPublic);
            var             providerField        = nodeType.GetField("Provider", INSTANCE_FLAGS);
            var             nextField            = nodeType.GetField("Next",     INSTANCE_FLAGS);
            var             typeDataField        = reflectionProvider.GetField("_typeData", INSTANCE_FLAGS);
            HashSet<object> visited              = [with(ReferenceEqualityComparer.Instance)];

            foreach (var firstNode in providerTable.Values.Cast<object>().Concat(providerTypeTable.Values.Cast<object>()))
            {
                var node = firstNode;

                while (node != null && visited.Add(node))
                {
                    var provider = providerField.GetValue(node);
                    if (reflectionProvider.IsInstanceOfType(provider))
                        removed += RemoveEntries((IDictionary)typeDataField.GetValue(provider), assemblies);

                    node = nextField.GetValue(node);
                }
            }

            removed += RemoveEntries(providerTable,        assemblies);
            removed += RemoveEntries(providerTypeTable,    assemblies);
            removed += RemoveEntries(initializedProviders, assemblies);

            string[] reflectionCaches = ["s_propertyCache", "s_eventCache", "s_attributeCache", "s_extendedPropertyCache"];

            foreach (var fieldName in reflectionCaches)
            {
                if (reflectionProvider.GetField(fieldName, STATIC_FLAGS).GetValue(null) is IDictionary cache)
                    removed += RemoveEntries(cache, assemblies);
            }
        }

        if (reflectionProvider.GetField("s_intrinsicTypeConverters", STATIC_FLAGS).GetValue(null) is IDictionary converters)
        {
            lock (converters)
                removed += RemoveEntries(converters, assemblies);
        }

        return removed;
    }

    private static IDictionary GetStore
    (
        object store
    ) =>
        (IDictionary)store.GetType().GetField("_concurrentStore", INSTANCE_FLAGS).GetValue(store);

    private static int RemoveEntries
    (
        IDictionary            cache,
        IReadOnlySet<Assembly> assemblies
    )
    {
        var removed = 0;

        foreach (var key in cache.Keys.Cast<object>().ToArray())
        {
            if (!ReferencesAssembly(key, assemblies) && !ReferencesAssembly(cache[key], assemblies)) continue;

            cache.Remove(key);
            removed++;
        }

        return removed;
    }

    private static bool ReferencesAssembly
    (
        object                 value,
        IReadOnlySet<Assembly> assemblies
    ) => value switch
    {
        null => false,
        Type type => assemblies.Contains(type.Assembly) ||
                     (type.HasElementType && ReferencesAssembly(type.GetElementType(), assemblies)) ||
                     (type.IsConstructedGenericType && type.GenericTypeArguments.Any(argument => ReferencesAssembly(argument, assemblies))),
        MemberInfo member       => assemblies.Contains(member.Module.Assembly) || ReferencesAssembly(member.DeclaringType, assemblies),
        ParameterInfo parameter => ReferencesAssembly(parameter.Member, assemblies) || ReferencesAssembly(parameter.ParameterType, assemblies),
        _ when value.GetType() is { IsConstructedGenericType: true } valueType &&
               valueType.GetGenericTypeDefinition().FullName == "Newtonsoft.Json.Utilities.StructMultiKey`2" =>
            ReferencesAssembly(valueType.GetField("Value1", INSTANCE_FLAGS).GetValue(value), assemblies) ||
            ReferencesAssembly(valueType.GetField("Value2", INSTANCE_FLAGS).GetValue(value), assemblies),
        _ => assemblies.Contains(value.GetType().Assembly)
    };

    #region 常量

    private const BindingFlags STATIC_FLAGS   = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags INSTANCE_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    #endregion
}
