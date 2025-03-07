using Microsoft.AspNetCore.Mvc;
using TokenServer.Handlers;

namespace TokenServer.Controllers;

public class ServersController : ControllerBase {
    [HttpPost("/servers/aika_get_chrcnt.asp")]
    public async Task<IActionResult> Aika_get_chrcnt([FromForm] string id, [FromForm] string pw) {
        string responseText = await AuthHandlers.GetCharacterCountAsync(id, pw);

        Response.StatusCode = 200;
        Response.Headers.Connection = "close";
        Response.Headers.ContentType = "text/html; charset=utf-8";
        Response.Headers["Content-Length"] = responseText.Length.ToString();
        Response.Headers.Date = DateTime.UtcNow.ToString("R");

        return Content(responseText, "text/html", System.Text.Encoding.UTF8);
    }

    [HttpPost("/servers/serv00.asp")]
    public string Serv00() {
        return AuthHandlers.GetPlayerCountPerServer();
    }

    [HttpPost("/servers/aika_reset_flag.asp")]
    public async Task Aika_reset_flag([FromForm] string id, [FromForm] string pw) {
        Console.WriteLine($"ResetFlag Id: {id}");
        await AuthHandlers.ResetFlag(id, pw);
    }
}
