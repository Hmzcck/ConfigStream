using ConfigStream.Core.Models;
using ConfigStream.Core.Services;

namespace ConfigStream.Core.Tests;

public class TypeConverterServiceTests
{
    private readonly TypeConverterService _service = new();

    [Theory]
    [InlineData("Hello World", ConfigurationType.String, typeof(string), "Hello World")]
    [InlineData("123", ConfigurationType.Number, typeof(int), 123)]
    [InlineData("4567890123", ConfigurationType.Number, typeof(long), 4567890123L)]
    [InlineData("3.14", ConfigurationType.Number, typeof(double), 3.14)]
    [InlineData("true", ConfigurationType.Boolean, typeof(bool), true)]
    [InlineData("false", ConfigurationType.Boolean, typeof(bool), false)]
    public void Convert_ReturnsExpectedResult_ForValidInputs(string input, ConfigurationType type, Type targetType,
        object expected)
    {
        var result = _service.Convert(input, type, targetType);
        Assert.Equal(expected, result);
        Assert.IsType(expected.GetType(), result);
    }

    [Fact]
    public void Convert_ThrowsInvalidCastException_ForUnsupportedNumberType()
    {
        Assert.Throws<InvalidCastException>(() =>
            _service.Convert("123", ConfigurationType.Number, typeof(DateTime)));
    }

    [Fact]
    public void Convert_ThrowsNotSupportedException_ForUnsupportedConfigurationType()
    {
        Assert.Throws<NotSupportedException>(() =>
            _service.Convert("test", (ConfigurationType)999, typeof(string)));
    }

    [Fact]
    public void Convert_ParsesJsonStringCorrectly()
    {
        string json = "{\"Name\":\"test\",\"Value\":123}";
        var result = _service.Convert(json, ConfigurationType.Json, typeof(TestJsonClass));
        Assert.NotNull(result);
        var obj = Assert.IsType<TestJsonClass>(result);
        Assert.Equal("test", obj.Name);
        Assert.Equal(123, obj.Value);
    }

    class TestJsonClass
    {
        public string Name { get; set; } = default!;
        public int Value { get; set; }
    }
}
