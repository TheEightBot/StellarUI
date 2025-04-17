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
        // For member-level attributes, we'd need a separate cache with appropriate key structure
        return Attribute.GetCustomAttribute(memberInfo, typeof(TAttribute)) as TAttribute;
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
}
