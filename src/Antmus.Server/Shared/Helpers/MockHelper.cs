using System.Text.Json;

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

        foreach (var file in Directory.GetFiles(this.mocksPath, "*.json"))
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

    public Response this[Request request]
    {
        get
        {
            if (request == null) throw new ArgumentNullException("Request");
            var item = this.Values.FirstOrDefault(f => f.Item1 == request.Method && f.Item2 == request.Hash);

            if (item == null) throw new KeyNotFoundException($"{request.Method}/{request.Hash}");

            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(item.Item3));

            return entry!.Response;
        }
    }

    public async Task Save(Request request, Response response)
    {
        var entry = new { Request = request, Response = response };

        var json = JsonSerializer.Serialize(entry, options: new() { WriteIndented = true });

        CreateDirectory();

        var filePath = Path.Combine(this.mocksPath, $"{entry.Request.Method}_{request.Hash}.json");
        
        log.LogInformation("Writing mock {filePath}", filePath);

        await File.WriteAllTextAsync(filePath, json);
    }
}
