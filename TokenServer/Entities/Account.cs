namespace TokenServer.Entities;

public class Account {
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Token { get; set; }
    public DateTime TokenCreationTime { get; set; }
    public int AccountStatus { get; set; }
    public int BanDays { get; set; }
    public int Nation { get; set; }
    public ICollection<Character> Characters { get; set; }
    public AccountType AccountType { get; set; }
}

public enum AccountType {
    Player,
    Founder,
    Sponser,
    Moderator,
    GameMaster,
    Admin
}