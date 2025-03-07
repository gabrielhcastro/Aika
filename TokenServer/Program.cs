using TokenServer.Handlers.Middleware;

namespace TokenServer;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseKestrel()
            .UseUrls("http://*:8090/");

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.AddServerHeader = false;
        });

        builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options => {
            options.HttpsPort = null;
        });

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        if(!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseExceptionMiddleware();

        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "member",
            pattern: "member/{action}.asp",
            defaults: new { controller = "Member" });

        app.MapControllerRoute(
            name: "servers",
            pattern: "servers/{action}.asp",
            defaults: new { controller = "Servers" });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
