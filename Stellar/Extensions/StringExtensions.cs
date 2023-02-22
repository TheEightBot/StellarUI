namespace Stellar;

public static class StringExtensions
{
    public static bool Contains(this string s, string innerString, StringComparison comparisonType)
    {
        return s.IndexOf(innerString, comparisonType) >= 0;
    }

    public static bool IsNullOrEmpty(this string s)
    {
        return string.IsNullOrEmpty(s);
    }

    public static bool IsNotNullOrEmpty(this string s)
    {
        return !string.IsNullOrEmpty(s);
    }

    public static bool IsNullOrWhiteSpace(this string s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    public static bool IsNotNullOrWhiteSpace(this string s)
    {
        return !string.IsNullOrWhiteSpace(s);
    }
}
