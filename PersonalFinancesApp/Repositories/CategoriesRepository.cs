using System.Text.Json;
namespace PersonalFinances.Repositories;

public class CategoriesRepository : ICategoriesRepository
{
    private string _categoriesJsonFilePath;

    public CategoriesRepository(string categoriesJsonFilePath)
    {
        _categoriesJsonFilePath = categoriesJsonFilePath;
    }

    public Dictionary<string, string> LoadCategoriesMap()
    {
        if (File.Exists(_categoriesJsonFilePath))
        {
            string json = File.ReadAllText(_categoriesJsonFilePath);
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

    public void SaveCategoriesMap(Dictionary<string, string> map)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(map, options);
        File.WriteAllText(_categoriesJsonFilePath, json); 
    }
}
