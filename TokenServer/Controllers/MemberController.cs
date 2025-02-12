using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;

namespace TokenServer.Controllers;

public class MemberController : ControllerBase {
    [HttpPost("/member/Aika_get_token.asp")]
    public string Aika_get_token(string id, string pw) {
        return AuthHandlers.GetToken(id, pw);
    }
}
