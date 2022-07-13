namespace Antmus.Server
{
    public class MockEngine : BaseEngine, IHttpEngine
    {
        private readonly MockHelper Mocks;
        private readonly CustomMockHelper CustomMocks;

        public MockEngine(ILogger<RecorderEngine> log, IConfiguration configuration, MockHelper mockHelper, CustomMockHelper customMockHelper) : base(log, configuration)
        {
            this.Mocks = mockHelper;
            this.CustomMocks = customMockHelper;
        }

        public async Task Handle(HttpContext context)
        {
            var request = GetRequestIdentifier(context.Request);
            Log.LogDebug(request);
            Log.LogInformation("Retrieving mock for {method} {path}", request.Method, request.Path);

            try
            {
                var mock = Mocks[request];
                var customMock = CustomMocks[request];

                var response = mock ?? customMock;
                if (response == null) throw new KeyNotFoundException();

                await CreateResponse(context, request, response);
            }
            catch (KeyNotFoundException ex)
            {
                Log.LogError("Key not found {key} {path}", ex.Message, request.Path);
                context.Response.StatusCode = 404;
            }
            catch(Exception ex)
            {
                var errorMessage = $"Failed to retrieve mock for {request.Method} {request.Path}";

                Log.LogError(errorMessage, ex);
                Log.LogDebug(ex, ex.Message);
                Log.LogTrace(request.ToString());

                context.Response.StatusCode = 503;
            }
        }
    }
}
