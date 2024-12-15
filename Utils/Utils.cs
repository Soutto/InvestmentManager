using System.Collections;

namespace InvestmentManager.Utils
{
    public static class Utils
    {
        private static bool IsNullOrEmpty<T>(T value)
        {
            if (value == null) return true;

            if (value is ICollection collection)
                return collection.Count == 0;

            if (value is IEnumerable<object> enumerable)
                return !enumerable.Any();
            
            return false;
        }
    }
}