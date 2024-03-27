namespace PersonalFinances.Extentions;

public static class DictionaryExtensions 
{
    // source: https://codereview.stackexchange.com/questions/8992/linq-approach-to-flatten-dictionary-to-string
    public static string ToString(this Dictionary<string,double> source, string keyValueSeparator, string sequenceSeparator)
    {
    if (source == null)
        throw new ArgumentException("Parameter source can not be null.");

    var pairs = source.Select(x => string.Format("{0}{1}{2}", x.Key, keyValueSeparator, x.Value.ToString("0.00")));

    return string.Join(sequenceSeparator, pairs).ToUpper();
    }

}