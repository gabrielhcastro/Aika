namespace Shared.Models.Account;
public class AccountDto {
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Token { get; set; }
    public DateTime TokenCreationTime { get; set; }
    public int AccountStatus { get; set; }
    public int BanDays { get; set; }
    public AccountTypeDto AccountType { get; set; }
    public int Cash { get; set; }
    public string PremiumExpiration { get; set; }
}

public enum AccountTypeDto : byte {
    Player = 0,
    Founder,
    Sponser,
    Moderator,
    GameMaster,
    Admin
}