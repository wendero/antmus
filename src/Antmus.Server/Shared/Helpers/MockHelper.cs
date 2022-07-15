using System.Text.Encodings.Web;

namespace Antmus.Server;

public class MockHelper
{
    private List<Tuple<string, string, string>> Values { get; set; } = new List<Tuple<string, string, string>>();

    private readonly ILogger<MockHelper> log;
    private readonly string mocksPath;

    public MockHelper(IConfiguration configuration, ILogger<MockHelper> log)
    {
        this.log = log;
        this.mocksPath = configuration.GetValue<string>("Mocks:Path") ?? "mocks";

        ReadMocks();
    }

    private void ReadMocks()
    {
        CreateDirectory();

        var files = Directory.GetFiles(this.mocksPath, "*.json").Where(w => !w.EndsWith(".custom.json"));

        foreach (var file in files)
        {
            var nameSplit = Path.GetFileNameWithoutExtension(file).Split('_');
            var method = nameSplit[0];
            var hash = nameSplit[1];

            this.Values.Add(Tuple.Create(method, hash, file));
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

            var item = this.Values.FirstOrDefault(f => f.Item1 == identifier.Method && f.Item2 == identifier.Hash);

            if (item == null) return null;

            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(item.Item3));

            return entry!.Response;
        }
    }

    public async Task Save(RequestIdentifier request, Response response)
    {
        var entry = new { Request = request.GetRequest(), Response = response };

        var json = JsonSerializer.Serialize(entry, JsonHelper.Options.Indented);

        CreateDirectory();

        var filename = $"{entry.Request.Method}_{request.Hash}.json";
        var filePath = Path.Combine(this.mocksPath, filename);
        
        log.LogInformation("Writing mock {filePath}", filePath);

        await File.WriteAllTextAsync(filePath, json);
    }
}
