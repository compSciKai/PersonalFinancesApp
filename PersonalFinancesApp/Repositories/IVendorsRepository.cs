namespace PersonalFinances.Repositories;

public interface IVendorsRepository
{
    IEnumerable<string> GetVendors(string path);
    void SaveVendors(string path);
}
