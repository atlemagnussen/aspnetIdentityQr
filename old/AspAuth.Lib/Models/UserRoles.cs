using System.Text.Json.Serialization;

namespace AspAuth.Lib.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRoles
{
    Admin
}