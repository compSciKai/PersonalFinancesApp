using System.Text.Json;
namespace PersonalFinances.Repositories;

public class VendorsRepository : IVendorsRepository
{
    private string _vendorsJsonFilePath;

    public VendorsRepository(string vendersJsonPath)
    {
        _vendorsJsonFilePath = vendersJsonPath;
    }

    public Dictionary<string, string> LoadVendorsMap()
    {
        if (File.Exists(_vendorsJsonFilePath))
        {
            string json = File.ReadAllText(_vendorsJsonFilePath);
            if (json != "")
            {
                var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (map is not null)
                {
                    return map;
                }
            }
        }

        return new Dictionary<string, string>();
    }

    public void SaveVendorsMap(Dictionary<string, string> map)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(map, options);
        File.WriteAllText(_vendorsJsonFilePath, json); 
    }
}
