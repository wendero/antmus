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
        var request = GetRequest(context.Request);
        log.LogInformation("Recording mock for {method} {path}", request.Method, request.Path);

        try
        {
            var response = await GetResponse(request, context.Request);

            await mock.Save(request, response);
            await CreateResponse(context, request, response);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to record mock {request.Method} {request.Path}";

            log.LogError(errorMessage, ex);
            log.LogDebug(ex, ex.Message);
            log.LogTrace(request.ToString());

            context.Response.StatusCode = 403;
            await context.Response.WriteAsync(errorMessage);
        }
    }

    private async Task<Response> GetResponse(Request request, HttpRequest httpRequest)
    {
        log.LogInformation("Retrieving response for {method} {path}", request.Method, request.Path);

        using var client = new HttpClient();
        var message = new HttpRequestMessage(new HttpMethod(request.Method), $"{baseUrl}{request.Path}");
        var contentType = (httpRequest.ContentType!).Split(';').First();

        if (httpRequest.Body.Length > 0)
        {
            httpRequest.Body.Position = 0;
            message.Content = new StreamContent(httpRequest.Body);
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        }

        foreach (var requestHeader in httpRequest.Headers)
        {
            if(this.RequestHeadersConfig.Contains(requestHeader.Key))
                client.DefaultRequestHeaders.Add(requestHeader.Key, requestHeader.Value.First());
        }

        var response = await client.SendAsync(message);
        var responseHeaders = response.Headers.Where(w => responseHeadersConfig?.Contains(w.Key) ?? false).ToDictionary(k => k.Key, v => v.Value.First()) ?? new Dictionary<string, string>();
        var type = response.Content?.Headers?.ContentType?.MediaType ?? "";
        var isJson = type == "application/json";
        var jsonContent = isJson ? response.Content!.ReadAsStringAsync().Result : null;
        var rawContent = !isJson ? ConvertByteArrayToHexString(response.Content!.ReadAsByteArrayAsync().Result) : null;
        var content = (isJson ? jsonContent : rawContent)!;

        return new Response(type, responseHeaders, (int)(response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError), content);
    }
}