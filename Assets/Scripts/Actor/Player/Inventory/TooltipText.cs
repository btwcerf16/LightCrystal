using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class TooltipText
{
    public static string Format(object source, string template, object fallback = null)
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
                object next = type.GetField(memberName)?.GetValue(value)
                    ?? type.GetProperty(memberName)?.GetValue(value);
                if (next == null && ReferenceEquals(value, source) && fallback != null)
                    next = fallback.GetType().GetField(memberName)?.GetValue(fallback)
                        ?? fallback.GetType().GetProperty(memberName)?.GetValue(fallback);
                value = next;
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
