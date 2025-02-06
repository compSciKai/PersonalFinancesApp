namespace PersonalFinancesApp.Api.Models
{
    public class User
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public  byte[] created_at { get; set; } = new byte[0];

        public User(int id, string name, byte[] createdAt)
        {
            Id = id;
            Name = name;
            created_at = createdAt;
        }

        public User()
        {
            
        }

    }
}
