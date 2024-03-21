namespace PersonalFinances.Repositories;

public interface IVendorsRepository
{
    void SaveVendorsMap(string path, Dictionary<string, string> map);
    Dictionary<string, string> LoadVendorsMap(string path);
}
