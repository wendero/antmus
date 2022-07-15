namespace Antmus.Server.Shared.Helpers;

public static class HashHelper
{
    public static string GetHash(Stream content)
    {
        var hash = System.Security.Cryptography.SHA1.Create().ComputeHashAsync(content);
        var hex = string.Join("", hash.Result.Select(s => s.ToString("X2")));

        return hex;
    }
    public static string GetHash(string? content)
    {
        var stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(content ?? ""));
        return GetHash(stream);
    }
    public static string GetHash(Dictionary<string, string> dictionary)
    {
        var requestHeaders = dictionary.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);
        return GetHash(JsonSerializer.Serialize(requestHeaders));
    }
}
