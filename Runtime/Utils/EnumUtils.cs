using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace PostLib.Utils
{
    public static class EnumUtils
    {
        public static T ParseEnumByValue<T>(string value) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attr && attr.Value == value)
                    return (T)field.GetValue(null);
            }

            throw new ArgumentException($"Requested value '{value}' was not found in enum {typeof(T).Name}");
        }
        public static string GetEnumMemberValue<T>(this T enumValue) where T : Enum
        {
            return typeof(T)
                .GetField(enumValue.ToString())
                ?.GetCustomAttribute<EnumMemberAttribute>()
                ?.Value ?? enumValue.ToString();
        }
    }
}