using TokenServer.Handlers.Middleware;

namespace TokenServer;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost
            .UseKestrel()
            .UseSetting("https_port", "8090")
            .UseUrls("https://*:8090/");

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        if(!app.Environment.IsDevelopment()) {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseExceptionMiddleware();

        app.UseHttpsRedirection();
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
