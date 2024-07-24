using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JPSoftworks.Text.Json;

public class UnixDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
    private const string DateTimeStartGuard = "/Date(";
    private const string DateTimeEndGuard = ")/";
    private const int MillisecondsPerTick = 10000;

    private readonly bool _specialHandlingForMinMax;

    public UnixDateTimeConverter(bool specialHandlingForMinMax = false)
    {
        _specialHandlingForMinMax = specialHandlingForMinMax;
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value) ||
            !value.StartsWith(DateTimeStartGuard, StringComparison.Ordinal) ||
            !value.EndsWith(DateTimeEndGuard, StringComparison.Ordinal))
        {
            throw new JsonException($"Invalid DateTime format: {value}");
        }

        var ticksValue = value.Substring(DateTimeStartGuard.Length, value.Length - DateTimeStartGuard.Length - DateTimeEndGuard.Length);
        var dateTimeKind = DateTimeKind.Utc;
        var indexOfTimeZoneOffset = ticksValue.IndexOf('+', 1);

        if (indexOfTimeZoneOffset == -1)
        {
            indexOfTimeZoneOffset = ticksValue.IndexOf('-', 1);
        }

        if (indexOfTimeZoneOffset != -1)
        {
            dateTimeKind = DateTimeKind.Local;
            ticksValue = ticksValue.Substring(0, indexOfTimeZoneOffset);
        }

        if (!long.TryParse(ticksValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var millisecondsSinceUnixEpoch))
        {
            throw new JsonException($"Error parsing DateTime ticks: {ticksValue}");
        }

        var ticks = millisecondsSinceUnixEpoch * MillisecondsPerTick + UnixEpochTicks;

        if (ticks > DateTime.MaxValue.Ticks || ticks < DateTime.MinValue.Ticks)
        {
            throw new JsonException($"DateTime value is out of range: {value}");
        }

        try
        {
            var dateTime = new DateTime(ticks, DateTimeKind.Utc);
            return dateTimeKind switch
            {
                DateTimeKind.Local => dateTime.ToLocalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime.ToLocalTime(), DateTimeKind.Unspecified),
                DateTimeKind.Utc => dateTime,
                _ => dateTime
            };
        }
        catch (ArgumentException ex)
        {
            throw new JsonException($"Error converting ticks to DateTime: {ticksValue}", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (_specialHandlingForMinMax)
        {
            if (value == DateTime.MaxValue || value == DateTime.MinValue)
            {
                writer.WriteStringValue(value == DateTime.MaxValue ? "DateTime.MaxValue" : "DateTime.MinValue");
                return;
            }
        }

        var tickCount = value.Ticks;
        if (value.Kind is not DateTimeKind.Utc)
        {
            tickCount -= TimeZoneInfo.Local.GetUtcOffset(value).Ticks;
            if (tickCount > DateTime.MaxValue.Ticks || tickCount < DateTime.MinValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "DateTime value is out of range.");
            }
        }

        var unixTimeMilliseconds = (value.ToUniversalTime().Ticks - UnixEpochTicks) / MillisecondsPerTick;
        var formattedDate = $"{DateTimeStartGuard}{unixTimeMilliseconds}";

        if (value.Kind is not DateTimeKind.Utc)
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(value.ToLocalTime());
            var sign = offset.Ticks < 0 ? "-" : "+";
            var hours = Math.Abs(offset.Hours).ToString("D2", CultureInfo.InvariantCulture);
            var minutes = Math.Abs(offset.Minutes).ToString("D2", CultureInfo.InvariantCulture);
            formattedDate += $"{sign}{hours}{minutes}";
        }

        formattedDate += DateTimeEndGuard;
        writer.WriteStringValue(formattedDate);
    }
}