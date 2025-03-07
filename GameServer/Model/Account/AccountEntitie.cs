using GameServer.Model.Character;

namespace GameServer.Model.Account;
public class AccountEntitie {
    public uint Id { get; set; }
    public uint ConnectionId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Token { get; set; }
    public DateTime TokenCreationTime { get; set; }
    public ushort AccountStatus { get; set; }
    public ushort BanDays { get; set; }
    public ushort Nation { get; set; }
    public AccountType AccountType { get; set; }
    public uint StorageGold { get; set; }
    public uint Cash { get; set; }
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