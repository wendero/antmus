namespace Antmus.Server;

public class RecorderEngine : BaseEngine, IHttpEngine
{
    private readonly MockHelper mock;
    private readonly string baseUrl;
    private readonly string[] responseHeadersConfig;

    public RecorderEngine(ILogger<RecorderEngine> log, IConfiguration configuration, MockHelper mock) : base(log, configuration)
    {
        this.mock = mock;
        this.baseUrl = configuration.GetValue<string>("Redirect");
        this.responseHeadersConfig = configuration.GetSection("ResponseHeaders").Get<string[]>();
    }

    public async Task Handle(HttpContext context)
    {
        var request = GetRequest(context.Request, out RequestIdentifier identifier);

        Log.LogDebug(request);
        Log.LogInformation("Recording mock for {method} {path}", request.Method, request.Path);

        try
        {
            var response = await GetResponse(request, context.Request);

            await mock.Save(request, response);
            await CreateResponse(context, RequestIdentifier.Create(request), response);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to record mock {request.Method} {request.Path}";

            Log.LogError(errorMessage, ex);
            Log.LogDebug(ex, ex.Message);

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(errorMessage);
        }
    }

    private async Task<Response> GetResponse(Request request, HttpRequest httpRequest)
    {
        Log.LogDebug(request);
        Log.LogInformation("Retrieving response for {method} {path}", request.Method, request.Path);

        using var client = new HttpClient();
        var message = new HttpRequestMessage(new HttpMethod(request.Method), $"{baseUrl}{request.Path}");
        var contentType = httpRequest.ContentType?.Split(';').First();

        if (contentType is not null)
        {
            httpRequest.Body.Position = 0;
            message.Content = new StreamContent(httpRequest.Body);
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        foreach (var requestHeader in httpRequest.Headers)
        {
            if (this.RequestHeadersConfig.Contains(requestHeader.Key))
                client.DefaultRequestHeaders.Add(requestHeader.Key, requestHeader.Value.First());
        }

        var response = await client.SendAsync(message);
        var responseHeaders = response.Headers.Where(w => responseHeadersConfig?.Contains(w.Key) ?? false).ToDictionary(k => k.Key, v => v.Value.First()) ?? new Dictionary<string, string>();
        var type = response.Content?.Headers?.ContentType?.MediaType ?? "";
        var isText = IsTextType(type);
        var stringContent = isText ? response.Content!.ReadAsStringAsync().Result : null;
        var rawContent = !isText ? ConvertByteArrayToHexString(response.Content!.ReadAsByteArrayAsync().Result) : null;
        var content = (isText ? stringContent : rawContent)!;

        return new Response { Type = type, Headers = responseHeaders, StatusCode = (int)(response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), Content = content };
    }
}