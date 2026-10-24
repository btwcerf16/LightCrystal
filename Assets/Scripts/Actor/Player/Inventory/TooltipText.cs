using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class TooltipText
{
    public static string Format(object source, string template)
    {
        if (string.IsNullOrEmpty(template))
            return "";

        return Regex.Replace(template, @"\{([\w.]+)(?::([^}]+))?\}", match =>
        {
            object value = source;
            foreach (string memberName in match.Groups[1].Value.Split('.'))
            {
                if (value == null)
                    return match.Value;
                Type type = value.GetType();
                value = type.GetField(memberName)?.GetValue(value)
                    ?? type.GetProperty(memberName)?.GetValue(value);
            }
            if (value == null)
                return match.Value;

            string format = match.Groups[2].Success ? match.Groups[2].Value : null;
            try
            {
                return value is IFormattable number
                    ? number.ToString(format, CultureInfo.CurrentCulture)
                    : value.ToString();
            }
            catch (FormatException)
            {
                return value.ToString();
            }
        });
    }
}
