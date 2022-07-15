namespace Antmus.Server.Engines;

public record RequestIdentifier
{
    public RequestIdentifier() { }

    public string Method { get; set; } = "";
    public string Path { get; set; } = "";

    public string PathHash { get; set; } = "";
    public string Hash { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string HeadersHash { get; set; } = "";

    public static RequestIdentifier Create(string method, string path, string body, Dictionary<string, string> requestHeaders)
    {
        var hashBody = HashHelper.GetHash(body);
        var hashPath = HashHelper.GetHash(path);
        var hashHeaders = HashHelper.GetHash(requestHeaders);
        var hash = HashHelper.GetHash(hashBody + hashPath + hashHeaders);

        var identifier = new RequestIdentifier
        {
            Path = path,
            Method = method,
            ContentHash = hashBody,
            PathHash = hashPath,
            HeadersHash = hashHeaders,
            Hash = hash
        };
        return identifier;
    }
    public static RequestIdentifier Create(Request request)
    { 
        var hashPath = HashHelper.GetHash(request.Path);
        var hashBody = HashHelper.GetHash(request.Content);
        var hashHeaders = HashHelper.GetHash(request.Headers);
        var hash = HashHelper.GetHash(hashBody + hashPath + hashHeaders);

        var identifier = new RequestIdentifier
        {
            Path = request.Path,
            Method = request.Method,
            PathHash = hashPath,
            ContentHash = request.Filters.Contains(nameof(Request.Content).ToLower()) ? hashBody : "",
            HeadersHash = request.Filters.Contains(nameof(Request.Headers).ToLower()) ? hashHeaders : "",
            Hash = hash
        };
        return identifier;
    }
}
public record Response
{
    public string Type { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public int StatusCode { get; set; }
    public string? Content { get; set; } = null;
}

public record Entry(Request Request, Response Response);
public record Request
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? Content { get; set; } = null;
    public string Type { get; set; } = "";
    public string Hash { get; set; } = "";

    public IEnumerable<string> Filters { get; set; } = Enumerable.Empty<string>();
}
