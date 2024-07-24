using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JPSoftworks.Text.Json;

public class UnixNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private readonly UnixDateTimeConverter _innerConverter;

    public UnixNullableDateTimeConverter(bool specialHandlingForMinMax = false)
    {
        _innerConverter = new(specialHandlingForMinMax);
    }

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _innerConverter.Read(ref reader, typeof(DateTime), options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            _innerConverter.Write(writer, value.Value, options);
        }
    }
}