namespace PacketTool;

internal static class Program {
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main() {
        if(OperatingSystem.IsWindowsVersionAtLeast(6, 1)) {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new PacketToolDesign());
        }
        else {
            Console.WriteLine("This application is only supported on Windows 6.1 and later.");
        }
    }
}
