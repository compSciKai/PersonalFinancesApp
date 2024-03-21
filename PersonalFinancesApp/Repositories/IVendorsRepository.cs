namespace PersonalFinances.Repositories;

public interface IVendorsRepository
{
    void SaveVendorsMap(Dictionary<string, string> map);
    Dictionary<string, string> LoadVendorsMap();
}
