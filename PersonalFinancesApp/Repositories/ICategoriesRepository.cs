namespace PersonalFinances.Repositories;

public interface ICategoriesRepository
{
    void SaveCategoriesMap(Dictionary<string, string> map);
    Dictionary<string, string> LoadCategoriesMap();
}
