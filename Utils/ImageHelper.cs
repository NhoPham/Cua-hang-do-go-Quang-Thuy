using System.Text.Json;

namespace QLBH.Utils;

public static class ImageHelper
{
    public static List<string> GetImageList(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new List<string>();
        }

        var normalized = rawValue.Trim();

        try
        {
            if (normalized.StartsWith("["))
            {
                return JsonSerializer.Deserialize<List<string>>(normalized) ?? new List<string>();
            }
        }
        catch
        {
        }

        if (normalized.Contains(','))
        {
            return normalized
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        return new List<string> { normalized };
    }

    public static string GetFirstImageOrDefault(string? rawValue, string defaultPath = "/images/default/no-image.jpg")
    {
        var images = GetImageList(rawValue);
        return images.FirstOrDefault() ?? defaultPath;
    }
}
