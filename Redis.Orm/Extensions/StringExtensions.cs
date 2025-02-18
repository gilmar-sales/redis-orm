namespace Redis.Orm.Extensions;

public static class StringExtensions
{
    public static int GetDeterministicHashCode(this string value)
    {
        unchecked
        {
            var hash = 23;

            foreach (var c in value) hash = hash * 31 + c;

            return hash;
        }
    }
}