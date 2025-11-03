using System.Text.Json;
using System.Text.Json.Serialization;

namespace tHerdBackend.SYS.Rcl.Areas.SYS.Controllers
{
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() ?? "",
                JsonTokenType.Number => reader.GetDouble().ToString(),
                JsonTokenType.Null => "",
                _ => reader.GetString() ?? ""
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
