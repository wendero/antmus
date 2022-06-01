namespace Antmus.Server
{
    public class MockEngine : BaseEngine, IHttpEngine
    {
        private readonly MockHelper mock;

        public MockEngine(ILogger<RecorderEngine> log, IConfiguration configuration, MockHelper configReader) : base(log, configuration)
        {
            this.mock = configReader;
        }

        public async Task Handle(HttpContext context)
        {
            var request = GetRequest(context.Request);
            log.LogInformation("Retrieving mock for {method} {path}", request.Method, request.Path);

            try
            {
                var response = mock[request];

                await CreateResponse(context, request, response);
            }
            catch (KeyNotFoundException ex)
            {
                log.LogError("Key not found {key} {path}", ex.Message, request.Path);
                context.Response.StatusCode = 404;
            }
            catch(Exception ex)
            {
                var errorMessage = $"Failed to retrieve mock for {request.Method} {request.Path}";

                log.LogError(errorMessage, ex);
                log.LogDebug(ex, ex.Message);
                log.LogTrace(request.ToString());

                context.Response.StatusCode = 503;
            }
        }
    }
}
