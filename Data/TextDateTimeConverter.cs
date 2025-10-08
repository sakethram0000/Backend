using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MyWebApi.Data;

public class TextDateTimeConverter : ValueConverter<DateTime, string>
{
    public TextDateTimeConverter() : base(
        dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff"),
        text => string.IsNullOrEmpty(text) ? DateTime.MinValue : DateTime.Parse(text))
    {
    }
}

public class TextNullableDateTimeConverter : ValueConverter<DateTime?, string>
{
    public TextNullableDateTimeConverter() : base(
        dateTime => dateTime.HasValue ? dateTime.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff") : null,
        text => string.IsNullOrEmpty(text) ? null : DateTime.Parse(text))
    {
    }
}