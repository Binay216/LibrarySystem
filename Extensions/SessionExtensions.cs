using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace LibrarySystem.Extensions
{
    /// <summary>ISession only stores strings/bytes natively - these round-trip a whole object as JSON under one key.</summary>
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value) =>
            session.SetString(key, JsonSerializer.Serialize(value));

        public static T? GetObject<T>(this ISession session, string key)
        {
            var json = session.GetString(key);
            return json is null ? default : JsonSerializer.Deserialize<T>(json);
        }
    }
}
