//Extensions/SessionExtensions.cs

using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WellUp.CustomerPortal
{
    public static class SessionExtensions
    {
        private static JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public static void Set<T>(this ISession session, string key, T value)
        {
            var options = GetSerializerOptions();
            session.SetString(key, JsonSerializer.Serialize(value, options));
        }

        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (string.IsNullOrEmpty(value))
                return default;

            var options = GetSerializerOptions();
            return JsonSerializer.Deserialize<T>(value, options);
        }
    }
}