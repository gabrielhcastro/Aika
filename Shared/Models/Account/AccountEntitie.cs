namespace Shared.Models.Account;
public class AccountEntitie {
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Token { get; set; }
    public DateTime TokenCreationTime { get; set; }
    public int AccountStatus { get; set; }
    public int BanDays { get; set; }
    public int Nation { get; set; }
    public AccountType AccountType { get; set; }
    public int StorageGold { get; set; }
    public int Cash { get; set; }
    public string PremiumExpiration { get; set; }
    public ICollection<CharacterEntitie> Characters { get; set; }
}