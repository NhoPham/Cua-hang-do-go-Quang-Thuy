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
                var jsonImages = JsonSerializer.Deserialize<List<string>>(normalized) ?? new List<string>();

                return jsonImages
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .ToList();
            }
        }
        catch
        {
        }

        var images = normalized
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (images.Any())
        {
            return images;
        }

        return new List<string> { normalized };
    }

    public static string GetFirstImageOrDefault(string? rawValue, string defaultPath = "/images/default/no-image.jpg")
    {
        var images = GetImageList(rawValue);
        return images.FirstOrDefault() ?? defaultPath;
    }
}