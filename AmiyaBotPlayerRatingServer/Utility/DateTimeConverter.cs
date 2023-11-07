using System.Text.Json.Serialization;
using System.Text.Json;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()??"");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // 如果DateTime是Unspecified或者Local，将其转换为UTC时间
            if (value.Kind == DateTimeKind.Unspecified || value.Kind == DateTimeKind.Local)
            {
                // 假设Unspecified是Local时间，因此先转换为Local，再转换为UTC
                value = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Local));
            }

            // 现在value是UTC时间，将其以ISO 8601字符串格式写入
            writer.WriteStringValue(value.ToString("O"));
        }

    }
}
