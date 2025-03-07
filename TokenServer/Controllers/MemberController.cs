using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;
using TokenServer.Models;

namespace TokenServer.Controllers;

public class MemberController : ControllerBase {
    [HttpPost("/member/aika_get_token.asp")]
    public async Task<IActionResult> Aika_get_token([FromForm] string id, [FromForm] string pw) {
        string responseText = await AuthHandlers.GetTokenAsync(id, pw);
        Response.Headers["Connection"] = "close";
        Response.Headers["Content-Type"] = "text/html; charset=utf-8";
        Response.Headers["Content-Length"] = responseText.Length.ToString();
        Response.Headers["Date"] = DateTime.UtcNow.ToString("R");
        return Content(responseText, "text/html", System.Text.Encoding.UTF8);
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
