using Microsoft.AspNetCore.Mvc;

namespace Antmus.Server.Controllers;

[Route("_antmus")]
public class AntmusController : Controller
{
    private bool IsRecorder { get; }
    private MockHelper Mocks { get; }
    private CustomMockHelper CustomMocks { get; }
    private ILogger<AntmusController> Log { get; }

    private readonly MockEngine _mockEngine;

    public AntmusController(IConfiguration configuration, CustomMockHelper customMockHelper, MockHelper mockHelper, ILogger<AntmusController> logger, MockEngine mockEngine)
    {
        this.IsRecorder = configuration?.GetValue<string>("Mode") == "Recorder";
        this.Mocks = mockHelper;
        this.CustomMocks = customMockHelper;
        this.Log = logger;
        
        _mockEngine = mockEngine;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMock([FromBody] MockProperties mock)
    {
        Log.LogDebug(mock);

        if (!this.IsRecorder) throw new AntmusModeNotRecorder();

        switch (mock.Type.ToLower())
        {
            case "custom":
                await this.CustomMocks.Save(mock.Request, mock.Response, mock.Name);
                break;
            case "default":
            case "mock":
                var identifier = RequestIdentifier.Create(mock.Request);
                mock.Request.Hash = identifier.Hash;

                await this.Mocks.Save(mock.Request, mock.Response);
                break;
            default:
                throw new InvalidMockType(mock.Type);
        }
        return StatusCode(201);
    }
}


internal class InvalidMockType : Exception { internal InvalidMockType(string type) : base($"Invalid mock type: {type}") { } }
internal class AntmusModeNotRecorder : Exception { internal AntmusModeNotRecorder() : base($"Antmus mode is not Recorder") { } }
public record MockProperties(string Type, string Name, RequestProperties Request, ResponseProperties Response);
public record RequestProperties : Request
{
    public new object? Content
    {
        get
        {
            return string.IsNullOrEmpty(base.Content) ? null : JsonSerializer.Deserialize<object?>(base.Content);
        }
        set
        {
            base.Content = JsonSerializer.Serialize(value, JsonHelper.Options.Minified);
        }
    }
}
public record ResponseProperties : Response
{
    public new object? Content
    {
        get
        {
            return string.IsNullOrEmpty(base.Content) ? null : JsonSerializer.Deserialize<object?>(base.Content);
        }
        set
        {
            base.Content = JsonSerializer.Serialize(value, JsonHelper.Options.Minified);
        }
    }
}
