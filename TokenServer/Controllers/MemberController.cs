using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;
using TokenServer.Models;

namespace TokenServer.Controllers;

public class MemberController : ControllerBase {
    [HttpPost("/member/Aika_get_token.asp")]
    public async Task<string> Aika_get_token(string id, string pw) {
        return await AuthHandlers.GetTokenAsync(id, pw);
    }

    [HttpPost("/member/create_account")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request) {
        var result = await AuthHandlers.CreateAccountAsync(request.Username, request.PasswordHash, request.AccountType);
        return result switch {
            "0" => BadRequest("Conta já existe."),
            "-99" => StatusCode(500, "Erro interno ao criar conta."),
            _ => Ok($"Conta criada com sucesso! ID: {result}")
        };
    }
}
