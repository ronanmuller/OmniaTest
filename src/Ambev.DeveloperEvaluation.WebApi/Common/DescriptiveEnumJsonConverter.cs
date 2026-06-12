using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ambev.DeveloperEvaluation.WebApi.Common;

public class DescriptiveEnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        var accepted = string.Join(", ", Enum.GetNames<T>());
        throw new JsonException($"Invalid value '{value}' for {typeof(T).Name}. Accepted values: {accepted}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

public class DescriptiveEnumJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(DescriptiveEnumJsonConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
