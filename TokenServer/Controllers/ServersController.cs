using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;

namespace TokenServer.Controllers;

public class ServersController : ControllerBase {
    [HttpPost("/servers/aika_get_chrcnt.asp")]
    public async Task<string> Aika_get_chrcnt(string id, string pw) {
        return await AuthHandlers.GetCharacterCountAsync(id, pw);
    }

    [HttpPost("/servers/serv00.asp")]
    public string Serv00() {
        return AuthHandlers.GetPlayerCountPerServer();
    }

    [HttpPost("/servers/aika_reset_flag.asp")]
    public async Task Aika_reset_flag(string id, string token) {
        await AuthHandlers.ResetFlag(id, token);
    }
}
