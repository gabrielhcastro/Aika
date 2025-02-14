namespace GameServer.GameLogic.Account;

public enum AccountType : byte {
    Player = 0,
    Founder,
    Sponser,
    Moderator,
    GameMaster,
    Admin
}