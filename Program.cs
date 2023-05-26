namespace LowBatteryAlert
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            //ApplicationConfiguration.Initialize();
            SettingsForm frm = new SettingsForm();
            frm.SettingsForm_Load(null, null);
            Application.Run();
        }
    }
}