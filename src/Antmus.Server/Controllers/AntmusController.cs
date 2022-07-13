using Microsoft.AspNetCore.Mvc;

namespace Antmus.Server.Controllers;

[Route("_antmus")]
public class AntmusController : Controller
{
    private bool IsRecorder { get; }
    private MockHelper Mocks { get; }
    private CustomMockHelper CustomMocks { get; }
    private ILogger<AntmusController> Log { get; }


    public AntmusController(IConfiguration configuration, CustomMockHelper customMockHelper, MockHelper mockHelper, ILogger<AntmusController> logger)
    {
        this.IsRecorder = configuration?.GetValue<string>("Mode") == "Recorder";
        this.Mocks = mockHelper;
        this.CustomMocks = customMockHelper;
        this.Log = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMock([FromBody]MockProperties mock)
    {
        Log.LogDebug(mock);

        if (!this.IsRecorder) throw new AntmusModeNotRecorder();

        switch(mock.Type.ToLower())
        {
            case "custom":
                await this.CustomMocks.Save(mock.Request, mock.Response, mock.Name);
                break;
            case "default":
            case "mock":
                await this.Mocks.Save(RequestIdentifier.Create(mock.Request), mock.Response);
                break;
            default:
                throw new InvalidMockType(mock.Type);
        }
        return StatusCode(201);
    }
}


internal class InvalidMockType : Exception { internal InvalidMockType(string type) : base($"Invalid mock type: {type}") { } }
internal class AntmusModeNotRecorder : Exception { internal AntmusModeNotRecorder() : base($"Antmus mode is not Recorder") { } }
public record MockProperties(string Type, string Name, CustomRequest Request, Response Response);