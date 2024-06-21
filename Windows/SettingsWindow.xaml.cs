using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;

namespace OrderManagerPlus.Windows
{
    public partial class SettingsWindow : Window
    {
        private AppSettings appSettings;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadDatabaseLocation();
        }

        private void LoadSettings()
        {
            appSettings = AppSettings.Load();
        }

        private void LoadDatabaseLocation()
        {
            if (string.IsNullOrWhiteSpace(appSettings.DatabasePath))
            {
                appSettings.DatabasePath = "ordermanagerplus.db";
                appSettings.Save();
            }
            DbLocationTextBox.Text = Path.GetFullPath(appSettings.DatabasePath);
        }

        private void SetDatabaseLocation(string dbPath)
        {
            appSettings.DatabasePath = dbPath;
            appSettings.Save();
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetDirectoryName(dbPath));
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow?.LoadDatabasePath(); // Оновлення шляху до бази даних у головному вікні
            mainWindow?.ReloadOrders(); // Перезавантаження даних з нової бази даних
            Logger.Log($"Шлях до бази даних оновлено: {dbPath}");
        }

        private void ReloadData()
        {
            using (var context = new Context())
            {
                var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow?.ReloadOrders();
            }
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("2024, Salvatore Lorenzzo aka gagawer ©\n\nПорада: не натискайте кнопку 'Об'єднати БД', якщо у вас немає машини часу :)");
        }

        private void ImportDbButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string newDbPath = openFileDialog.FileName;

                    // Перевірка структури нової бази даних
                    if (!CheckDatabaseStructure(newDbPath))
                    {
                        MessageBox.Show("Структура нової бази даних не відповідає очікуваній. Імпорт неможливий.");
                        Logger.Log("Спроба імпорту бази даних не вдалася через невідповідність структури.");
                        return;
                    }

                    SetDatabaseLocation(newDbPath);
                    ReloadData(); // Перезавантаження даних з нової бази даних
                    MessageBox.Show("База даних успішно імпортована.");
                    Logger.Log($"База даних успішно імпортована з файлу: {newDbPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка імпорту бази даних: {ex.Message}");
                    Logger.Log($"Помилка імпорту бази даних: {ex.Message}");
                }
            }
        }

        private void ExportDbButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*",
                FileName = "ordermanagerplus_backup.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(appSettings.DatabasePath, saveFileDialog.FileName, true);
                    MessageBox.Show("База даних успішно експортована.");
                    Logger.Log($"База даних успішно експортована до файлу: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка експорту бази даних: {ex.Message}");
                    Logger.Log($"Помилка експорту бази даних: {ex.Message}");
                }
            }
        }

        private void MergeDbButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceDbPath = openFileDialog.FileName;
                    string targetDbPath = appSettings.DatabasePath;

                    // Перевірка відповідності таблиць і стовпців
                    if (!CheckDatabaseStructure(sourceDbPath, targetDbPath))
                    {
                        MessageBox.Show("Структури баз даних не співпадають. Об'єднання неможливе.");
                        Logger.Log("Спроба об'єднання баз даних не вдалася через невідповідність структур.");
                        return;
                    }

                    // Об'єднання даних з таблиць
                    MergeDatabaseData(sourceDbPath, targetDbPath);
                    MessageBox.Show("База даних успішно об'єднана.");
                    Logger.Log($"База даних успішно об'єднана з файлом: {sourceDbPath}");
                    ReloadData(); // Перезавантаження даних після об'єднання
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка об'єднання баз даних: {ex.Message}");
                    Logger.Log($"Помилка об'єднання баз даних: {ex.Message}");
                }
            }
        }

        private bool CheckDatabaseStructure(string dbPath)
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var tables = GetTables(connection);

                // Перевірка наявності необхідних таблиць
                var requiredTables = new[] { "Customers", "Tasks", "Orders" };
                if (!requiredTables.All(t => tables.Contains(t)))
                {
                    return false;
                }

                // Перевірка наявності необхідних стовпців
                foreach (var table in requiredTables)
                {
                    var columns = GetColumns(connection, table);
                    if (table == "Customers" && !columns.Contains("FullName"))
                    {
                        return false;
                    }
                    if (table == "Tasks" && !columns.Contains("Name"))
                    {
                        return false;
                    }
                    if (table == "Orders" && !columns.Contains("Price"))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CheckDatabaseStructure(string sourceDbPath, string targetDbPath)
        {
            using (var sourceConnection = new SqliteConnection($"Data Source={sourceDbPath}"))
            using (var targetConnection = new SqliteConnection($"Data Source={targetDbPath}"))
            {
                sourceConnection.Open();
                targetConnection.Open();

                var sourceTables = GetTables(sourceConnection);
                var targetTables = GetTables(targetConnection);

                if (!sourceTables.SequenceEqual(targetTables))
                {
                    return false;
                }

                foreach (var table in sourceTables)
                {
                    var sourceColumns = GetColumns(sourceConnection, table);
                    var targetColumns = GetColumns(targetConnection, table);

                    if (!sourceColumns.SequenceEqual(targetColumns))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private string[] GetTables(SqliteConnection connection)
        {
            var tables = new System.Collections.Generic.List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }
            return tables.ToArray();
        }

        private string[] GetColumns(SqliteConnection connection, string table)
        {
            var columns = new System.Collections.Generic.List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({table});";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(1));
                    }
                }
            }
            return columns.ToArray();
        }

        private void MergeDatabaseData(string sourceDbPath, string targetDbPath)
        {
            using (var sourceConnection = new SqliteConnection($"Data Source={sourceDbPath}"))
            using (var targetConnection = new SqliteConnection($"Data Source={targetDbPath}"))
            {
                sourceConnection.Open();
                targetConnection.Open();

                var tables = GetTables(sourceConnection);

                foreach (var table in tables)
                {
                    var columns = GetColumns(sourceConnection, table);
                    var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));

                    using (var selectCommand = sourceConnection.CreateCommand())
                    {
                        selectCommand.CommandText = $"SELECT {columnList} FROM {table}";
                        using (var reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var values = new object[reader.FieldCount];
                                reader.GetValues(values);

                                using (var insertCommand = targetConnection.CreateCommand())
                                {
                                    var placeholders = string.Join(", ", values.Select((_, i) => $"@p{i}"));
                                    insertCommand.CommandText = $"INSERT OR IGNORE INTO {table} ({columnList}) VALUES ({placeholders})";

                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        insertCommand.Parameters.AddWithValue($"@p{i}", values[i]);
                                    }

                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new Context())
                {
                    context.Database.EnsureCreated();
                }
                MessageBox.Show("База даних дійсна та доступна.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка перевірки бази даних: {ex.Message}");
            }
        }

        private void CreateNewDbButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "SQLite Database Files (*.db)|*.db|All Files (*.*)|*.*",
                FileName = "ordermanagerplus.db"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        File.Delete(saveFileDialog.FileName);
                    }
                    using (var context = new Context())
                    {
                        context.Database.EnsureCreated();
                    }
                    SetDatabaseLocation(saveFileDialog.FileName); // Оновлюємо шлях до нової бази даних
                    MessageBox.Show("Нова база даних успішно створена.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка створення нової бази даних: {ex.Message}");
                }
            }
        }

        private void ShowShortcutsButton_Click(object sender, RoutedEventArgs e)
        {
            string shortcuts = @"Гарячі клавіші:
Ctrl + C: Додати замовника
Ctrl + T: Додати завдання
Ctrl + O: Додати замовлення
Ctrl + I: Відкрити налаштування
Ctrl + S: Відкрити статистику
Ctrl + P: Відкрити оплату
Ctrl + 1: Встановити статус Не виконано
Ctrl + 2: Встановити статус Частково виконано
Ctrl + 3: Встановити статус Виконано/не оплачено
Ctrl + 4: Встановити статус Виконано і оплачено";

            MessageBox.Show(shortcuts, "Гарячі клавіші");
        }
    }
}

public class AppSettings
{
    public string DatabasePath { get; set; }

    private static string settingsFile = "appsettings.json";

    public static AppSettings Load()
    {
        if (File.Exists(settingsFile))
        {
            var json = File.ReadAllText(settingsFile);
            return JsonConvert.DeserializeObject<AppSettings>(json);
        }
        return new AppSettings();
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(settingsFile, json);
    }
}