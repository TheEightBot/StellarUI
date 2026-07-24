using System.Reactive;
using System.Reactive.Linq;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// The filter and projection helpers on IObservableExtensions. Individually trivial,
/// but several pairs differ only by name -- WhereIsTrue filters while ValueIsTrue
/// projects -- so a mix-up would change behaviour without breaking compilation.
/// </summary>
public class ObservablePredicateExtensionsTests
{
    private static List<T> Collect<T>(IObservable<T> observable)
    {
        var results = new List<T>();
        using var subscription = observable.Subscribe(results.Add);
        return results;
    }

    [Fact]
    public void SelectUnit_ProjectsEveryValueToUnit()
    {
        var results = Collect(new[] { 1, 2, 3 }.ToObservable().SelectUnit());

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.Equal(Unit.Default, r));
    }

    [Fact]
    public void AsObject_BoxesValues()
    {
        var results = Collect(new[] { 1, 2 }.ToObservable().AsObject());

        Assert.Equal(new object?[] { 1, 2 }, results);
    }

    [Fact]
    public void IsNotNull_KeepsOnlyNonNullReferences()
    {
        var results = Collect(new string?[] { "a", null, "b" }.ToObservable().IsNotNull());

        Assert.Equal(new[] { "a", "b" }, results);
    }

    [Fact]
    public void IsNull_KeepsOnlyNulls()
    {
        var results = Collect(new string?[] { "a", null, "b", null }.ToObservable().IsNull());

        Assert.Equal(2, results.Count);
        Assert.All(results, Assert.Null);
    }

    [Fact]
    public void IsDefault_KeepsDefaultValues()
    {
        var results = Collect(new[] { 0, 1, 0, 2 }.ToObservable().IsDefault());

        Assert.Equal(new[] { 0, 0 }, results);
    }

    [Fact]
    public void IsNotDefault_DropsDefaultValues()
    {
        var results = Collect(new[] { 0, 1, 0, 2 }.ToObservable().IsNotDefault());

        Assert.Equal(new[] { 1, 2 }, results);
    }

    [Fact]
    public void WhereHasValue_KeepsOnlyNullablesWithAValue()
    {
        var results = Collect(new int?[] { 1, null, 0, null }.ToObservable().WhereHasValue());

        // 0 has a value, so it survives; only the nulls are dropped.
        Assert.Equal(new int?[] { 1, 0 }, results);
    }

    [Fact]
    public void WhereHasValueAndIs_MatchesTheComparison()
    {
        var results = Collect(new int?[] { 1, 2, null, 2 }.ToObservable().WhereHasValueAndIs(2));

        Assert.Equal(new int?[] { 2, 2 }, results);
    }

    [Fact]
    public void WhereHasValueAndIsNot_ExcludesTheComparisonAndNulls()
    {
        var results = Collect(new int?[] { 1, 2, null, 3 }.ToObservable().WhereHasValueAndIsNot(2));

        Assert.Equal(new int?[] { 1, 3 }, results);
    }

    [Fact]
    public void WhereHasValueAndIsNotDefault_ExcludesNullsAndDefaults()
    {
        var results = Collect(new int?[] { 0, 1, null, 2 }.ToObservable().WhereHasValueAndIsNotDefault());

        Assert.Equal(new int?[] { 1, 2 }, results);
    }

    [Fact]
    public void IsNotEmpty_DropsEmptyStrings()
    {
        var results = Collect(new[] { "a", string.Empty, "b" }.ToObservable().IsNotEmpty());

        Assert.Equal(new[] { "a", "b" }, results);
    }

    [Fact]
    public void IsNotNullOrEmpty_DropsBothNullAndEmpty()
    {
        var results = Collect(new string?[] { "a", null, string.Empty, "b" }.ToObservable().IsNotNullOrEmpty());

        Assert.Equal(new[] { "a", "b" }, results);
    }

    [Fact]
    public void IsNotNull_OnStrings_KeepsEmptyButDropsNull()
    {
        var results = Collect(new string?[] { "a", null, string.Empty }.ToObservable().IsNotNull());

        // Distinct from IsNotNullOrEmpty: an empty string is not null.
        Assert.Equal(new[] { "a", string.Empty }, results);
    }

    [Fact]
    public void GetValueOrDefault_SubstitutesForNulls()
    {
        var results = Collect(new int?[] { 1, null, 3 }.ToObservable().GetValueOrDefault(99));

        Assert.Equal(new[] { 1, 99, 3 }, results);
    }

    [Fact]
    public void GetValueOrDefault_WithoutAnExplicitDefault_UsesTypeDefault()
    {
        var results = Collect(new int?[] { 1, null }.ToObservable().GetValueOrDefault());

        Assert.Equal(new[] { 1, 0 }, results);
    }

    [Fact]
    public void WhereIsTrue_Filters()
    {
        var results = Collect(new[] { true, false, true }.ToObservable().WhereIsTrue());

        Assert.Equal(new[] { true, true }, results);
    }

    [Fact]
    public void WhereIsFalse_Filters()
    {
        var results = Collect(new[] { true, false, false }.ToObservable().WhereIsFalse());

        Assert.Equal(new[] { false, false }, results);
    }

    [Fact]
    public void ValueIsTrue_ProjectsRatherThanFiltering()
    {
        var results = Collect(new[] { true, false, true }.ToObservable().ValueIsTrue());

        // Every value survives; this is identity, not a filter.
        Assert.Equal(new[] { true, false, true }, results);
    }

    [Fact]
    public void ValueIsFalse_InvertsEveryValueRatherThanFiltering()
    {
        var results = Collect(new[] { true, false, true }.ToObservable().ValueIsFalse());

        Assert.Equal(new[] { false, true, false }, results);
    }

    [Fact]
    public void WhereIs_KeepsMatchingValues()
    {
        var results = Collect(new[] { "a", "b", "a" }.ToObservable().WhereIs("a"));

        Assert.Equal(new[] { "a", "a" }, results);
    }

    [Fact]
    public void WhereIsNot_DropsMatchingValues()
    {
        var results = Collect(new[] { "a", "b", "a" }.ToObservable().WhereIsNot("a"));

        Assert.Equal(new[] { "b" }, results);
    }

    [Fact]
    public void TakeOne_YieldsOnlyTheFirstValue()
    {
        var results = Collect(new[] { 1, 2, 3 }.ToObservable().TakeOne());

        Assert.Equal(new[] { 1 }, results);
    }

    [Fact]
    public void SkipOne_DropsOnlyTheFirstValue()
    {
        var results = Collect(new[] { 1, 2, 3 }.ToObservable().SkipOne());

        Assert.Equal(new[] { 2, 3 }, results);
    }
}
