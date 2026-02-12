using System.Reflection;
using System.Text.Json.Serialization;

namespace UserAdmin.Extensions;

public static class EnumExtensions
{
    public static string GetJsonName<T>(this T enumValue) where T : struct, Enum
    {
        var name = enumValue.ToString();
        var field = typeof(T).GetField(name);

        if (field != null)
        {
            var attribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            if (attribute != null)
            {
                return attribute.Name;
            }
        }

        return name;
    }
}