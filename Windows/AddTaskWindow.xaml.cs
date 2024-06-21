using System;
using System.Linq;
using System.Windows;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;

namespace OrderManagerPlus.Windows
{
    public partial class AddTaskWindow : Window
    {
        private Task _selectedTask;

        public AddTaskWindow()
        {
            InitializeComponent();
            this.Loaded += AddTaskWindow_Loaded;
        }

        private void AddTaskWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            TaskListBox.ItemsSource = null;
        }

        private void LoadCategories()
        {
            var categories = SQLiteDataAccess.GetTasks().Select(t => t.Category).Distinct().ToList();
            CategoryComboBox.ItemsSource = categories;
        }

        private void LoadTasks(string category = null)
        {
            var tasks = string.IsNullOrEmpty(category) ?
                SQLiteDataAccess.GetTasks().OrderBy(t => t.Category).ThenBy(t => t.Name).ToList() :
                SQLiteDataAccess.GetTasks().Where(t => t.Category == category).OrderBy(t => t.Name).ToList();

            TaskListBox.ItemsSource = tasks;
        }

        private void CategoryComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem != null)
            {
                LoadTasks(CategoryComboBox.SelectedItem.ToString());
            }
            else
            {
                TaskListBox.ItemsSource = null;
            }
        }

        private void CategoryRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ExistingCategoryRadioButton != null && ExistingCategoryRadioButton.IsChecked == true)
            {
                if (CategoryComboBox != null)
                {
                    CategoryComboBox.Visibility = Visibility.Visible;
                    if (CategoryComboBox.SelectedItem != null)
                    {
                        LoadTasks(CategoryComboBox.SelectedItem.ToString());
                    }
                }
                if (NewCategoryTextBox != null)
                {
                    NewCategoryTextBox.Visibility = Visibility.Collapsed;
                }
            }
            else if (NewCategoryRadioButton != null && NewCategoryRadioButton.IsChecked == true)
            {
                if (CategoryComboBox != null)
                {
                    CategoryComboBox.Visibility = Visibility.Collapsed;
                }
                if (NewCategoryTextBox != null)
                {
                    NewCategoryTextBox.Visibility = Visibility.Visible;
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string category = ExistingCategoryRadioButton != null && ExistingCategoryRadioButton.IsChecked == true ? CategoryComboBox.SelectedItem?.ToString().Trim() : NewCategoryTextBox.Text.Trim();
            string name = NameTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Будь ласка, оберіть або введіть категорію.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Поле назви завдання не може бути пустим.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price))
            {
                MessageBox.Show("Невірна ціна.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (price < 0)
            {
                MessageBox.Show("Ціна не може бути від'ємною.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existingTask = SQLiteDataAccess.GetTasks().FirstOrDefault(t => t.Category == category && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existingTask != null)
            {
                MessageBoxResult result = MessageBox.Show($"Завдання з назвою '{name}' вже існує у категорії '{category}'. Додати це завдання з номером, наприклад, '{name}(2)'?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    int duplicateIndex = 2;
                    string newName;
                    do
                    {
                        newName = $"{name}({duplicateIndex})";
                        duplicateIndex++;
                    }
                    while (SQLiteDataAccess.GetTasks().Any(t => t.Category == category && t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)));
                    name = newName;
                }
                else
                {
                    return;
                }
            }

            Task newTask = new Task
            {
                Category = category,
                Name = name,
                Description = description,
                Price = price
            };

            try
            {
                SQLiteDataAccess.AddTask(newTask);
                Logger.Log($"Додано нове завдання: {newTask.Name} у категорії {newTask.Category} з ціною {newTask.Price}");
                MessageBox.Show("Завдання успішно додано.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadCategories();

                ExistingCategoryRadioButton.IsChecked = true;
                CategoryComboBox.SelectedItem = category;
                LoadTasks(category);
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при додаванні нового завдання: {ex.Message}");
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask != null)
            {
                string category = ExistingCategoryRadioButton.IsChecked == true ? CategoryComboBox.SelectedItem?.ToString().Trim() : NewCategoryTextBox.Text.Trim();
                string name = NameTextBox.Text.Trim();
                string description = DescriptionTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(category))
                {
                    MessageBox.Show("Будь ласка, оберіть або введіть категорію.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Поле назви завдання не може бути пустим.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text, out decimal price))
                {
                    MessageBox.Show("Невірна ціна.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (price < 0)
                {
                    MessageBox.Show("Ціна не може бути від'ємною.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _selectedTask.Category = category;
                _selectedTask.Name = name;
                _selectedTask.Description = description;
                _selectedTask.Price = price;

                try
                {
                    SQLiteDataAccess.UpdateTask(_selectedTask);
                    Logger.Log($"Оновлено завдання: {_selectedTask.Name} у категорії {_selectedTask.Category} з новою ціною {_selectedTask.Price}");
                    MessageBox.Show("Завдання успішно оновлено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (ExistingCategoryRadioButton.IsChecked == true)
                    {
                        LoadTasks(category);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Помилка при оновленні завдання: {ex.Message}");
                    MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTask != null)
            {
                var orders = SQLiteDataAccess.GetOrders().Where(o => o.TaskId == _selectedTask.Id).ToList();
                if (orders.Any(o => o.Status == "Не виконано" || o.Status == "Частково виконано"))
                {
                    var result = MessageBox.Show("Для цього завдання є незавершені замовлення. Видалити їх?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            foreach (var order in orders)
                            {
                                SQLiteDataAccess.DeleteOrder(order.Id);
                                Logger.Log($"Видалено замовлення з ID: {order.Id} для завдання: {_selectedTask.Name}");
                            }
                            SQLiteDataAccess.DeleteTask(_selectedTask.Id);
                            Logger.Log($"Видалено завдання: {_selectedTask.Name} у категорії {_selectedTask.Category}");
                            MessageBox.Show("Завдання і пов'язані з ним замовлення успішно видалені.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Помилка при видаленні завдання або його замовлень: {ex.Message}");
                            MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Неможливо видалити завдання з незавершеними замовленнями.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {  
                    try
                    {
                        SQLiteDataAccess.DeleteTask(_selectedTask.Id);
                        Logger.Log($"Видалено завдання: {_selectedTask.Name} у категорії {_selectedTask.Category}");
                        MessageBox.Show("Завдання успішно видалене.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Помилка при видаленні завдання: {ex.Message}");
                        MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    
                }

                LoadTasks(CategoryComboBox.SelectedItem?.ToString());
            }
        }

        private void TaskListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedTask = (Task)TaskListBox.SelectedItem;
            if (_selectedTask != null)
            {
                if (ExistingCategoryRadioButton != null && ExistingCategoryRadioButton.IsChecked == true)
                {
                    CategoryComboBox.SelectedItem = _selectedTask.Category;
                }
                else if (NewCategoryRadioButton != null && NewCategoryRadioButton.IsChecked == true)
                {
                    NewCategoryTextBox.Text = _selectedTask.Category;
                }
                NameTextBox.Text = _selectedTask.Name;
                DescriptionTextBox.Text = _selectedTask.Description;
                PriceTextBox.Text = _selectedTask.Price.ToString();
            }
        }
    }
}