using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace JPSoftworks.Text.Json.Tests;

[TestClass]
public class UnixDateTimeConverterTests
{
    private JsonSerializerOptions? _jsonSerializerOptions;

    [TestInitialize]
    public void Initialize()
    {
        _jsonSerializerOptions = CreateJsonSerializerOptions(specialHandlingForMinMax: false);
    }

    private JsonSerializerOptions CreateJsonSerializerOptions(bool specialHandlingForMinMax = false)
    {
        var encoderSettings = new TextEncoderSettings();
        encoderSettings.AllowRange(UnicodeRanges.All);
        encoderSettings.AllowCharacter('+');

        return new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new UnixDateTimeConverter(specialHandlingForMinMax), new UnixNullableDateTimeConverter(specialHandlingForMinMax) }
        };
    }

    [TestMethod]
    public void Serialize_NullDateTime_ShouldReturnNull()
    {
        DateTime? dateTime = null;
        var json = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);
        Assert.AreEqual("null", json);
    }

    [TestMethod]
    public void Deserialize_NullDateTime_ShouldReturnNull()
    {
        var json = "null";
        var dateTime = JsonSerializer.Deserialize<DateTime?>(json, _jsonSerializerOptions);
        Assert.IsNull(dateTime);
    }

    [TestMethod]
    public void Serialize_NonNullableDateTime_ShouldMatchDataContractJsonSerializer()
    {
        var dateTime = new DateTime(2024, 7, 23, 12, 0, 0, DateTimeKind.Utc);
        var json1 = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);

        var dataContractSerializer = new DataContractJsonSerializer(typeof(DateTime));
        using var memoryStream = new MemoryStream();
        dataContractSerializer.WriteObject(memoryStream, dateTime);
        var json2 = Encoding.UTF8.GetString(memoryStream.ToArray()).Replace("\\/", "/");

        Assert.AreEqual(json2, json1);
    }

    [TestMethod]
    public void Deserialize_NonNullableDateTime_ShouldMatchDataContractJsonSerializer()
    {
        var json = """
                   "/Date(1724400000000+0000)/"
                   """;
        var dateTime1 = JsonSerializer.Deserialize<DateTime>(json, _jsonSerializerOptions);

        var dataContractSerializer = new DataContractJsonSerializer(typeof(DateTime));
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json.Replace("/", "\\/")));
        var dateTime2 = (DateTime)dataContractSerializer.ReadObject(memoryStream);

        Assert.AreEqual(dateTime2, dateTime1);
    }

    [TestMethod]
    public void Serialize_MaxValue_ShouldReturnCorrectString()
    {
        _jsonSerializerOptions = CreateJsonSerializerOptions(specialHandlingForMinMax: true);

        DateTime? dateTime = DateTime.MaxValue;
        var json = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);

        const string expected = """
                                "DateTime.MaxValue"
                                """;
        Assert.AreEqual(expected, json);
    }

    [TestMethod]
    public void Serialize_MinValue_ShouldReturnCorrectString()
    {
        _jsonSerializerOptions = CreateJsonSerializerOptions(specialHandlingForMinMax: true);

        DateTime? dateTime = DateTime.MinValue;
        var json = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);
        var expected = """
                       "DateTime.MinValue"
                       """;
        Assert.AreEqual(expected, json);
    }

    [TestMethod]
    [DynamicData(nameof(DatesToTest))]
    public void Serialize_LocalDateTime_ShouldMatchDataContractJsonSerializer(DateTime dateTime)
    {
        var json1 = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);

        var dataContractSerializer = new DataContractJsonSerializer(typeof(DateTime));
        using var memoryStream = new MemoryStream();
        dataContractSerializer.WriteObject(memoryStream, dateTime);
        var json2 = Encoding.UTF8.GetString(memoryStream.ToArray()).Replace("\\/", "/");

        Assert.AreEqual(json2, json1);
    }

    private static IEnumerable<object[]> DatesToTest =>
    [
        [new DateTime(2024, 7, 23, 12, 0, 0, DateTimeKind.Local)],
    ];

    [TestMethod]
    [DynamicData(nameof(NullableDatesToTest))]
    public void Serialize_LocalNullableDateTime_ShouldMatchDataContractJsonSerializer(DateTime? dateTime)
    {
        var json1 = JsonSerializer.Serialize(dateTime, _jsonSerializerOptions);

        var dataContractSerializer = new DataContractJsonSerializer(typeof(DateTime));
        using var memoryStream = new MemoryStream();
        dataContractSerializer.WriteObject(memoryStream, dateTime);
        var json2 = Encoding.UTF8.GetString(memoryStream.ToArray()).Replace("\\/", "/");

        Assert.AreEqual(json2, json1);
    }

    private static IEnumerable<object[]> NullableDatesToTest =>
    [
        [new DateTime(2024, 7, 23, 12, 0, 0, DateTimeKind.Local)],
        [null!],
    ];

    [TestMethod]
    public void Deserialize_LocalDateTime_ShouldMatchDataContractJsonSerializer()
    {
        var json = """
                   "/Date(1724400000000+0200)/"
                   """;
        var dateTime1 = JsonSerializer.Deserialize<DateTime>(json, _jsonSerializerOptions);

        var dataContractSerializer = new DataContractJsonSerializer(typeof(DateTime));
        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json.Replace("/", "\\/")));
        var dateTime2 = (DateTime)dataContractSerializer.ReadObject(memoryStream);

        Assert.AreEqual(dateTime2, dateTime1);
    }
}
