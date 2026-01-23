using System.Collections.Generic;

public static class TypeExtensions
{
    #region string
    public static bool IsNullOrEmpty(this string str)
    {
        return string.IsNullOrEmpty(str);
    }
    #endregion string
    
    #region IDictionary
    public static bool IsNullOrEmpty<TKey, TValue>(this IDictionary<TKey, TValue> dict)
    {
        return dict == null || dict.Count == 0;
    }
    #endregion IDictionary
}