using System.Text.Json;

namespace Antmus.Server;

public abstract class BaseEngine
{
    public readonly ILogger<RecorderEngine> log;

    public string[] RequestHeadersConfig { get; set; }

    public BaseEngine(ILogger<RecorderEngine> log, IConfiguration configuration)
    {
        this.log = log;
        this.RequestHeadersConfig = configuration.GetSection("RequestHeaders").Get<string[]>();
    }
    public Request GetRequest(HttpRequest request)
    {
        request.EnableBuffering();

        var path = request.Path;

        log.LogInformation("Request: {method} {path}", request.Method, path);

        var requestBody = request.Body;
        var requestHeaders = request.Headers
            .Where(w => RequestHeadersConfig.Contains(w.Key) || w.Key.ToLower().StartsWith("antmus"))
            .ToDictionary(k => k.Key, v => v.Value.First()) 
                ?? new Dictionary<string, string>();

        var hashBody = GetHash(requestBody);
        var hashPath = GetHash(path);
        var hashHeaders = GetHash(JsonSerializer.Serialize(requestHeaders));
        var hash = GetHash(hashBody + hashPath + hashHeaders);

        log.LogInformation("Request hash: {hash}", hash);

        return new Request(request.Method, path.Value ?? "", hash);
    }
    public static string GetHash(Stream content)
    {
        var hash = System.Security.Cryptography.SHA1.Create().ComputeHashAsync(content);
        var hex = string.Join("", hash.Result.Select(s => s.ToString("X2")));

        return hex;
    }
    public static string GetHash(string content)
    {
        var stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(content));
        return GetHash(stream);
    }
    public static string ConvertByteArrayToHexString(IEnumerable<byte>? byteArray)
    {
        if (byteArray == null)
            return "";
        return string.Join("", byteArray.Select(s => s.ToString("X2")));
    }
    public static IEnumerable<byte> ConvertHexStringToByteArray(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return Enumerable.Empty<byte>();

        var identify = hexString.Select((x, i) => new { Index = i, Value = x });
        var group = identify.GroupBy(x => x.Index / 2);
        var concatenate = group.Select(x => x.First().Value.ToString() + x.Last().Value.ToString());
        var convert = concatenate.Select(x => Convert.ToByte(x, 16));
        var result = convert.ToArray();

        return result;
    }
    public async Task CreateResponse(HttpContext context, Request request, Response response)
    {
        log.LogInformation("Sending response for {path} ({hash})", request.Path, request.Hash);

        var httpResponse = context.Response;

        httpResponse.StatusCode = response.StatusCode;

        foreach (var header in response.Headers)
        {
            httpResponse.Headers.Append(header.Key, header.Value);
        }

        var content = response.Content;

        if (content != null)
        {
            httpResponse.ContentType = response.Type;

            await httpResponse.WriteAsync(content);
        }
    }
}
public record Entry(Request Request, Response Response);
public record Response(string Type, Dictionary<string, string> Headers, int StatusCode, string Content);
public record Request(string Method, string Path, string Hash);
