using System;
using System.Linq;
using System.Windows;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;

namespace OrderManagerPlus.Windows
{
    public partial class AddCustomerWindow : Window
    {
        private Customer _selectedCustomer;

        public AddCustomerWindow()
        {
            InitializeComponent();
            this.Loaded += AddCustomerWindow_Loaded;
        }

        private void AddCustomerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
        }

        private void LoadCustomersByGroup(string group)
        {
            var customers = SQLiteDataAccess.GetCustomers()
                .Where(c => c.Group.Equals(group, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.FullName)
                .ToList();
            CustomerListBox.ItemsSource = customers;
        }

        private void LoadGroups()
        {
            if (GroupComboBox != null)
            {
                GroupComboBox.Items.Clear();
                var groups = SQLiteDataAccess.GetCustomers()
                    .Select(c => c.Group)
                    .Distinct()
                    .Where(g => !string.IsNullOrEmpty(g))
                    .ToList();

                foreach (var group in groups)
                {
                    GroupComboBox.Items.Add(group);
                }
            }
        }

        private void GroupComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GroupComboBox.SelectedItem != null)
            {
                LoadCustomersByGroup(GroupComboBox.SelectedItem.ToString());
            }
        }

        private void GroupRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ExistingGroupRadioButton != null && ExistingGroupRadioButton.IsChecked == true && GroupComboBox != null && NewGroupTextBox != null)
            {
                GroupComboBox.Visibility = Visibility.Visible;
                NewGroupTextBox.Visibility = Visibility.Collapsed;
            }
            else if (NewGroupRadioButton != null && NewGroupRadioButton.IsChecked == true && GroupComboBox != null && NewGroupTextBox != null)
            {
                GroupComboBox.Visibility = Visibility.Collapsed;
                NewGroupTextBox.Visibility = Visibility.Visible;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string fullName = NameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("Поле ПІБ не може бути пустим.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string group = ExistingGroupRadioButton != null && ExistingGroupRadioButton.IsChecked == true
                ? GroupComboBox.SelectedItem?.ToString()
                : NewGroupTextBox.Text.Trim();

            string notes = NotesTextBox.Text.Trim();

            if (ExistingGroupRadioButton != null && ExistingGroupRadioButton.IsChecked == true)
            {
                if (GroupComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Будь ласка, оберіть існуючу групу.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (NewGroupRadioButton != null && NewGroupRadioButton.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(NewGroupTextBox.Text))
                {
                    MessageBox.Show("Будь ласка, введіть нову назву групи.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (GroupComboBox.Items.Contains(group))
                {
                    MessageBox.Show("Ця група вже існує. Будь ласка, оберіть іншу назву.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var existingCustomer = SQLiteDataAccess.GetCustomers()
                .FirstOrDefault(c => string.Equals(c.FullName.Replace(" ", "").ToLower(), fullName.Replace(" ", "").ToLower()) &&
                                     string.Equals(c.Group, group, StringComparison.OrdinalIgnoreCase));

            if (existingCustomer != null)
            {
                MessageBoxResult result = MessageBox.Show($"Замовник з ПІБ '{fullName}' вже існує у групі '{group}'. Додати цього замовника з номером, наприклад, '{fullName}(2)'?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    int duplicateIndex = 2;
                    string newFullName;
                    do
                    {
                        newFullName = $"{fullName}({duplicateIndex})";
                        duplicateIndex++;
                    }
                    while (SQLiteDataAccess.GetCustomers().Any(c => string.Equals(c.FullName.Replace(" ", "").ToLower(), newFullName.Replace(" ", "").ToLower()) &&
                                                                    string.Equals(c.Group, group, StringComparison.OrdinalIgnoreCase)));
                    fullName = newFullName;
                }
                else
                {
                    return;
                }
            }

            Customer newCustomer = new Customer
            {
                FullName = fullName,
                Group = group,
                Notes = notes
            };

            try
            {
                SQLiteDataAccess.AddCustomer(newCustomer);
                Logger.Log($"Додано нового замовника: {newCustomer.FullName}, група: {newCustomer.Group}");
                MessageBox.Show("Замовник успішно доданий.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadGroups();
                GroupComboBox.SelectedItem = group;
                ExistingGroupRadioButton.IsChecked = true;
                LoadCustomersByGroup(group);
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при додаванні нового замовника: {ex.Message}");
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer != null)
            {
                _selectedCustomer.FullName = NameTextBox.Text.Trim();
                _selectedCustomer.Group = ExistingGroupRadioButton != null && ExistingGroupRadioButton.IsChecked == true ? GroupComboBox.SelectedItem?.ToString() : NewGroupTextBox.Text.Trim();
                _selectedCustomer.Notes = NotesTextBox.Text.Trim();

                try
                {
                    SQLiteDataAccess.UpdateCustomer(_selectedCustomer);
                    Logger.Log($"Оновлено дані замовника: {_selectedCustomer.FullName}, група: {_selectedCustomer.Group}");
                    MessageBox.Show("Замовник успішно оновлений.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCustomersByGroup(_selectedCustomer.Group);
                    LoadGroups();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Помилка при оновленні замовника: {ex.Message}");
                    MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCustomer != null)
            {
                var orders = SQLiteDataAccess.GetOrders().Where(o => o.CustomerId == _selectedCustomer.Id).ToList();
                if (orders.Any(o => o.Status == "Не виконано" || o.Status == "Частково виконано"))
                {
                    var result = MessageBox.Show("У замовника є незавершені замовлення. Видалити їх?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            foreach (var order in orders)
                            {
                                SQLiteDataAccess.DeleteOrder(order.Id);
                                Logger.Log($"Видалено замовлення з ID: {order.Id} для замовника: {_selectedCustomer.FullName}");
                            }
                            SQLiteDataAccess.DeleteCustomer(_selectedCustomer.Id);
                            Logger.Log($"Видалено замовника: {_selectedCustomer.FullName}, група: {_selectedCustomer.Group}");
                            MessageBox.Show("Замовник і пов'язані з ним завдання успішно видалені.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log($"Помилка при видаленні замовника або його замовлень: {ex.Message}");
                            MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Неможливо видалити замовника з незавершеними замовленнями.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        SQLiteDataAccess.DeleteCustomer(_selectedCustomer.Id);
                        Logger.Log($"Видалено замовника: {_selectedCustomer.FullName}, група: {_selectedCustomer.Group}");
                        MessageBox.Show("Замовник успішно видалений.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Помилка при видаленні замовника: {ex.Message}");
                        MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                LoadCustomersByGroup(GroupComboBox.SelectedItem?.ToString());
                LoadGroups();
            }
        }

        private void CustomerListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _selectedCustomer = (Customer)CustomerListBox.SelectedItem;
            if (_selectedCustomer != null)
            {
                NameTextBox.Text = _selectedCustomer.FullName;
                if (GroupComboBox != null && GroupComboBox.Items.Contains(_selectedCustomer.Group))
                {
                    if (ExistingGroupRadioButton != null)
                    {
                        ExistingGroupRadioButton.IsChecked = true;
                    }
                    GroupComboBox.SelectedItem = _selectedCustomer.Group;
                }
                else
                {
                    if (NewGroupRadioButton != null)
                    {
                        NewGroupRadioButton.IsChecked = true;
                    }
                    NewGroupTextBox.Text = _selectedCustomer.Group;
                }
                NotesTextBox.Text = _selectedCustomer.Notes;
            }
        }
    }
}