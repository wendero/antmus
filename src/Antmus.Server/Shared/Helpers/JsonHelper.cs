using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace Antmus.Server.Shared.Helpers
{
    public static class JsonHelper
    {
        public static string Minify(this string str)
        {
            var obj = JsonSerializer.Deserialize<object>(str);
            return JsonSerializer.Serialize(obj, Options.Minified);
        }
        public static class Options
        {
            public static JsonSerializerOptions Minified = new JsonSerializerOptions { WriteIndented = false, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            public static JsonSerializerOptions Indented = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        }
    }
}
