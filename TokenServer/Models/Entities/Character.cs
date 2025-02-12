namespace TokenServer.Models.Entities;

public class Character {
    public int Id { get; set; }
    public string Name { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; }
}