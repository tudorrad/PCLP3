namespace StoryEngine.Player;

internal static class EntryPoint
{
    private static readonly string LogPath = Path.Combine(
        Path.GetTempPath(), "StoryEngine_Player_crash.log");

    private static void Log(string text)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {text}\r\n"); }
        catch { /* ignora - logging e best-effort */ }
    }

    [STAThread]
    private static void Main()
    {
        Log("=== Pornire aplicatie (Main inceput) ===");

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Log("UnhandledException: " + ex);
            try
            {
                MessageBox.Show(
                    $"A apărut o eroare critică la pornire:\n\n{ex?.Message}\n\n" +
                    $"Detalii complete salvate în:\n{LogPath}",
                    "Eroare critică", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* ignora */ }
        };

        try
        {
            Log("ApplicationConfiguration.Initialize() ...");
            ApplicationConfiguration.Initialize();
            Log("ApplicationConfiguration OK");

            Application.ThreadException += (s, e) =>
            {
                Log("ThreadException: " + e.Exception);
                MessageBox.Show(
                    $"A apărut o eroare neașteptată:\n\n{e.Exception.Message}\n\n" +
                    $"Detalii complete salvate în:\n{LogPath}",
                    "Eroare aplicație", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Log("Creez MainForm...");
            var form = new MainForm();
            Log("MainForm creat cu succes.");

            Log("Pornesc Application.Run...");
            Application.Run(form);

            Log("Application.Run s-a terminat normal (fereastra a fost inchisa).");
        }
        catch (Exception ex)
        {
            Log("EXCEPTIE in Main: " + ex);
            MessageBox.Show(
                $"Aplicația nu a putut porni:\n\n{ex.Message}\n\n" +
                $"Detalii complete salvate în:\n{LogPath}",
                "Eroare la pornire", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        Log("=== Main s-a terminat ===");
    }
}
