using System.Text.Encodings.Web;

namespace Antmus.Server;

public class CustomMockHelper
{
    private Dictionary<RequestIdentifier, Response> Values { get; set; } = new();

    private readonly ILogger<MockHelper> log;
    private readonly string mocksPath;

    public CustomMockHelper(IConfiguration configuration, ILogger<MockHelper> log)
    {
        this.log = log;
        this.mocksPath = configuration.GetValue<string>("Mocks:Path") ?? "mocks";

        ReadMocks();
    }

    private void ReadMocks()
    {
        CreateDirectory();

        var files = Directory.GetFiles(this.mocksPath, "*.custom.json");

        foreach (var file in files)
        {
            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(file))!;
            this.Values.Add(RequestIdentifier.Create(entry.Request), entry.Response);
        }
    }

    private void CreateDirectory()
    {
        if (!Directory.Exists(mocksPath))
            Directory.CreateDirectory(mocksPath);
    }

    public Response? this[RequestIdentifier identifier]
    {
        get
        {
            if (identifier == null) throw new ArgumentNullException(nameof(identifier));
            
            //search for all filters: path, content, headers
            var mocksWithHash = this.Values.Where(w => w.Key.Hash == identifier.Hash);
            if (mocksWithHash.Any()) return mocksWithHash.First().Value;

            //search only for path filter
            var mocksWithPath = this.Values.Where(w => w.Key.PathHash == identifier.PathHash);
            if(!mocksWithPath.Any()) return null;
            
            //search for path and headers filters
            var mocksWithHeaders = mocksWithPath.Where(w => w.Key.HeadersHash == identifier.HeadersHash && w.Key.ContentHash == "");
            if(mocksWithHeaders.Any()) return mocksWithHeaders.First().Value;

            //search for path and content filters
            var mocksWithContent = mocksWithPath.Where(w => w.Key.ContentHash == identifier.ContentHash && w.Key.HeadersHash == "");
            if(mocksWithContent.Any()) return mocksWithContent.First().Value;

            //if after all there still have at least a Path
            var mocksWithOnlyPath = this.Values.Where(w => w.Key.PathHash == identifier.PathHash && w.Key.ContentHash == "" && w.Key.HeadersHash == "");
            if (mocksWithOnlyPath.Any()) return mocksWithOnlyPath.First().Value;
            
            return null;
        }
    }
    public async Task Save(Request request, Response response, string filename)
    {
        var entry = new Entry(request, response);

        var json = JsonSerializer.Serialize(entry, JsonHelper.Options.Indented);

        CreateDirectory();

        var filePath = Path.Combine(this.mocksPath, $"{filename}.custom.json");

        log.LogInformation("Writing custom mock {filePath}", filePath);

        await File.WriteAllTextAsync(filePath, json);
    }
}
