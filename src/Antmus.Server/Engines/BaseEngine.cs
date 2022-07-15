﻿namespace Antmus.Server;

public abstract class BaseEngine
{
    public readonly ILogger<RecorderEngine> Log;

    public IList<string> RequestHeadersConfig { get; set; }

    public BaseEngine(ILogger<RecorderEngine> log, IConfiguration configuration)
    {
        this.Log = log;
        this.RequestHeadersConfig = configuration.GetSection("RequestHeaders").Get<IList<string>>() ?? new List<string>();
    }
    public RequestIdentifier GetRequestIdentifier(HttpRequest request, string body, Dictionary<string, string> headers)
    {
        Log.LogDebug();

        var path = request.Path;

        Log.LogInformation("Request: {method} {path}", request.Method, path);

        var identifier = RequestIdentifier.Create(request.Method, path, body, headers);

        Log.LogInformation($"Request hash {identifier.Hash}");
        Log.LogJson("Request", new
        {
            Path = path,
            Method = request.Method,
            Headers = headers,
            Body = body
        });
        Log.LogJson("Request Identifier hashes:", identifier);

        return identifier;
    }
    public Request GetRequest(HttpRequest request, out RequestIdentifier identifier)
    {
        Log.LogDebug();

        var body = GetBodyAsString(request);
        var headers = GetRequestHeaders(request);

        identifier = this.GetRequestIdentifier(request, body, headers);

        return new Request { Content = body, Hash = identifier.Hash, Headers = headers, Method = identifier.Method, Path = identifier.Path };
    }
    private static string GetBodyAsString(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;

        if (IsJsonType(request.ContentType))
        {
            var stream = new StreamReader(request.Body);
            var json = stream.ReadToEndAsync().Result.Minify();
            return json;
        }

        var memoryStream = new MemoryStream();
        request.Body.CopyToAsync(memoryStream).Wait();
        byte[] byteArray = memoryStream.ToArray();

        return string.Join("", byteArray.Select(s => s.ToString("X2")));
    }

    private static Dictionary<string, string> GetRequestHeaders(HttpRequest request)
    {
        return request.Headers
            .Where(w => RequestHeadersConfig.Contains(w.Key) || w.Key.ToLower().StartsWith("antmus"))
            .OrderBy(o => o.Key)
            .ToDictionary(k => k.Key, v => v.Value.First())
                ?? new Dictionary<string, string>();
    }

    public static string ConvertByteArrayToHexString(IEnumerable<byte>? byteArray)
    {
        if (byteArray == null)
            return "";
        return string.Join("", byteArray.Select(s => s.ToString("X2")));
    }
    public static byte[] ConvertHexStringToByteArray(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return new byte[] { };

        var identify = hexString.Select((x, i) => new { Index = i, Value = x });
        var group = identify.GroupBy(x => x.Index / 2);
        var concatenate = group.Select(x => x.First().Value.ToString() + x.Last().Value.ToString());
        var convert = concatenate.Select(x => Convert.ToByte(x, 16));
        var result = convert.ToArray();

        return result;
    }
    public async Task CreateResponse(HttpContext context, RequestIdentifier requestIdentifier, Response response)
    {
        Log.LogDebug(new { requestIdentifier, response });
        Log.LogInformation("Sending response for {path} ({hash})", requestIdentifier.Path, requestIdentifier.Hash);

        var httpResponse = context.Response;
        httpResponse.StatusCode = response.StatusCode;

        foreach (var header in response.Headers)
        {
            httpResponse.Headers.Append(header.Key, header.Value);
        }

        if (!string.IsNullOrWhiteSpace(response.Type))
        {
            httpResponse.ContentType = response.Type;

            if (!IsTextType(response.Type))
            {
                await httpResponse.BodyWriter.WriteAsync(ConvertHexStringToByteArray(response.Content).ToArray());
            }
            else
            {
                await httpResponse.WriteAsync(response.Content);
            }
        }
    }
    private static string[] jsonTypes = new string[] { "application/json", "application/problem+json" };
    public static bool IsTextType(string mimeType)
        => mimeType is not null && (mimeType.StartsWith("text/") || IsJsonType(mimeType));
    public static bool IsJsonType(string mimeType)
        => mimeType is not null && jsonTypes.Contains(mimeType);
}