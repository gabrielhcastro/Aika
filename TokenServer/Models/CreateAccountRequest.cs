namespace TokenServer.Models;

public class CreateAccountRequest {
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public int AccountType { get; set; }
}