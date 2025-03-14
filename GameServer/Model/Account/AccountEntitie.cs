using GameServer.Model.Character;

namespace GameServer.Model.Account;
public class AccountEntitie {
    public ushort Id { get; set; }
    public ushort ConnectionId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Token { get; set; }
    public DateTime TokenCreationTime { get; set; }
    public byte AccountStatus { get; set; }
    public byte BanDays { get; set; }
    public byte Nation { get; set; }
    public AccountType AccountType { get; set; }
    public ulong StorageGold { get; set; }
    public ulong Cash { get; set; }
    public string PremiumExpiration { get; set; }
    public List<CharacterEntitie> Characters { get; set; } = [];
}

public enum AccountType : byte {
    Player = 0,
    Founder,
    Sponser,
    Moderator,
    GameMaster,
    Admin
}