using System;
using System.IO;
using System.Windows;

namespace OrderManagerPlus
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppSettings settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.DatabasePath) && File.Exists(settings.DatabasePath))
            {
                SetDatabasePath(settings.DatabasePath);
            }
            else
            {
                settings.DatabasePath = "ordermanagerplus.db";
                settings.Save();
                SetDatabasePath(settings.DatabasePath);
            }
        }

        private void SetDatabasePath(string dbPath)
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetDirectoryName(dbPath));
        }
    }
}