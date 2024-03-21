using System.Text.Json;
namespace PersonalFinances.Repositories;

public class VendorsRepository : IVendorsRepository
{
    public Dictionary<string, string> LoadVendorsMap(string path)
    {
        // TODO: check file type
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
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

    public void SaveVendorsMap(string path, Dictionary<string, string> map)
    {
        var json = JsonSerializer.Serialize(map);
        File.WriteAllText(path, json); 
    }
}
