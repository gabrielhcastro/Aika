using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;

namespace TokenServer.Controllers;

public class ServersController : ControllerBase {
    [HttpPost("/servers/aika_get_chrcnt.asp")]
    public string Aika_get_chrcnt(string id, string pw) {
        return AuthHandlers.GetCharacterCount(id, pw);
    }

    [HttpPost("/servers/serv00.asp")]
    public string Serv00() {
        return AuthHandlers.GetPlayerCountPerServer();
    }

    [HttpPost("/servers/aika_reset_flag.asp")]
    public string Aika_reset_flag(string id, string token) {
        return AuthHandlers.ResetFlag(id, token);
    }
}
