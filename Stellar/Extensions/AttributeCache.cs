namespace Stellar.Extensions;

using System;
using System.Collections.Concurrent;
using System.Reflection;

/// <summary>
/// High-performance cache for attribute lookups to avoid repeated reflection calls.
/// </summary>
public static class AttributeCache
{
    private static readonly ConcurrentDictionary<(Type TypeKey, Type AttributeType), Attribute?> TypeAttributeCache = new();
    private static readonly ConcurrentDictionary<(MemberInfo MemberKey, Type AttributeType), Attribute?> MemberAttributeCache = new();

    /// <summary>
    /// Gets a custom attribute for a type with high performance caching.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
    /// <param name="type">The type to check for the attribute.</param>
    /// <returns>The attribute instance or null if not found.</returns>
    public static TAttribute? GetAttribute<TAttribute>(Type type)
        where TAttribute : Attribute
    {
        return (TAttribute?)TypeAttributeCache.GetOrAdd(
            (type, typeof(TAttribute)),
            key => Attribute.GetCustomAttribute(key.TypeKey, key.AttributeType));
    }

    /// <summary>
    /// Gets a custom attribute for a member with high performance caching.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to retrieve.</typeparam>
    /// <param name="memberInfo">The member to check for the attribute.</param>
    /// <returns>The attribute instance or null if not found.</returns>
    public static TAttribute? GetAttribute<TAttribute>(MemberInfo memberInfo)
        where TAttribute : Attribute
    {
        return (TAttribute?)MemberAttributeCache.GetOrAdd(
            (memberInfo, typeof(TAttribute)),
            key => Attribute.GetCustomAttribute(key.MemberKey, key.AttributeType));
    }

    /// <summary>
    /// Checks if a type has a specific attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to check for.</typeparam>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the attribute exists.</returns>
    public static bool HasAttribute<TAttribute>(Type type)
        where TAttribute : Attribute
    {
        return GetAttribute<TAttribute>(type) != null;
    }

    /// <summary>
    /// Clears the cache to prevent memory leaks in long-running applications.
    /// Call this periodically or when you know types are being unloaded.
    /// </summary>
    public static void ClearCache()
    {
        TypeAttributeCache.Clear();
        MemberAttributeCache.Clear();
    }

    /// <summary>
    /// Gets the current cache statistics for monitoring memory usage.
    /// </summary>
    public static (int TypeCacheCount, int MemberCacheCount) GetCacheStatistics()
    {
        return (TypeAttributeCache.Count, MemberAttributeCache.Count);
    }
}
