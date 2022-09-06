using System.Net.Http.Headers;

namespace Antmus.Server;

public abstract class BaseEngine
{
    public readonly ILogger<RecorderEngine> Log;

    public IList<string> RequestHeadersToCalculateHashFrom { get; set; }

    public BaseEngine(ILogger<RecorderEngine> log, IConfiguration configuration)
    {
        this.Log = log;
        this.RequestHeadersToCalculateHashFrom = configuration.GetSection("RequestHeadersToCalculateHashFrom").Get<IList<string>>() ?? new List<string>();

        Log.LogJson("Request Headers", this.RequestHeadersToCalculateHashFrom, LogLevel.Information);
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
        var headersToCalculateHashFrom = GetRequestHeadersToCalculateHashFrom(request);

        identifier = this.GetRequestIdentifier(request, body, headersToCalculateHashFrom);

        return new Request { Content = body, Hash = identifier.Hash, Headers = headers, Method = identifier.Method, Path = identifier.Path };
    }
    private string GetBodyAsString(HttpRequest request)
    {
        string GetBodyAsOtherType(HttpRequest request)
        {
            var memoryStream = new MemoryStream();
            request.Body.CopyToAsync(memoryStream).Wait();
            byte[] byteArray = memoryStream.ToArray();

            var otherBody = string.Join("", byteArray.Select(s => s.ToString("X2")));
            Log.LogJson("Other body", otherBody);
            return otherBody;
        }

        Log.LogDebug(new { request.Method, request.Path, request.ContentType });
        request.EnableBuffering();
        request.Body.Position = 0;
        
        if(request.ContentType is null) return GetBodyAsOtherType(request);

        if (IsJsonType(request.ContentType))
        {
            var stream = new StreamReader(request.Body);
            var body = stream.ReadToEndAsync().Result;
            if (body.Length == 0)
            {
                Log.LogWarning("Missmatch: request's content type indicates body is in json format yet body is empty");
                return "";
            }

            var json = body.Minify();
            Log.LogJson("Json body", json);
            return json;
        }

        if (IsTextType(request.ContentType))
        {
            var textBody = new StreamReader(request.Body)
                .ReadToEnd();
            Log.LogJson("Text body", textBody);
            return textBody;
        }

        return GetBodyAsOtherType(request);
    }

    private Dictionary<string, string> GetRequestHeaders(HttpRequest request)
    {
        return request.Headers
            .OrderBy(o => o.Key)
            .ToDictionary(k => k.Key, v => v.Value.First())
                ?? new Dictionary<string, string>();
    }
    private Dictionary<string, string> GetRequestHeadersToCalculateHashFrom(HttpRequest request)
    {
        return request.Headers
            .Where(w => RequestHeadersToCalculateHashFrom.Contains(w.Key) || w.Key.ToLower().StartsWith("antmus"))
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

            if (!IsTextOrJsonType(response.Type))
            {
                await httpResponse.BodyWriter.WriteAsync(ConvertHexStringToByteArray(response.Content).ToArray());
            }
            else
            {
                await httpResponse.WriteAsync(response.Content);
            }
        }
    }
    public static bool IsTextType(string mimeType)
        => mimeType is not null && (mimeType.StartsWith("text/"));
    public static bool IsTextOrJsonType(string mimeType)
        => mimeType is not null && (mimeType.StartsWith("text/") || IsJsonType(mimeType));
    public static bool IsJsonType(string mimeType)
    {
        if (!MediaTypeHeaderValue.TryParse(mimeType, out var mt)) return false;

        if (mt.MediaType!.Equals("application/json", StringComparison.OrdinalIgnoreCase)) return true; // Matches application/json
        if (mt.MediaType.Contains("+json", StringComparison.OrdinalIgnoreCase)) return true; // Matches +json, e.g. application/ld+json

        return false;
    }
}