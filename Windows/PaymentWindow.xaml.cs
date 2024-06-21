using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;

namespace OrderManagerPlus.Windows
{
    public partial class PaymentWindow : Window
    {
        public event Action PaymentCompleted;

        private List<Customer> _allCustomers;

        public PaymentWindow()
        {
            InitializeComponent();
            LoadCustomers();
            LoadGroups();
        }

        private void LoadCustomers()
        {
            _allCustomers = SQLiteDataAccess.GetCustomers();
            CustomerListBox.ItemsSource = _allCustomers;
            CustomerListBox.DisplayMemberPath = "FullName";
        }

        private void LoadGroups()
        {
            var groups = _allCustomers.Select(c => c.Group).Distinct().ToList();
            groups.Insert(0, "-"); // Додати елемент "-" для відображення всіх замовників
            GroupComboBox.ItemsSource = groups;
            GroupComboBox.SelectedIndex = 0; // За замовчуванням обрати елемент "-"
        }

        private void CustomerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomerListBox.SelectedItem is Customer selectedCustomer)
            {
                BalanceTextBlock.Text = $"Баланс: {selectedCustomer.Balance:C}";
            }
            else
            {
                BalanceTextBlock.Text = string.Empty;
            }
        }

        private void GroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedGroup = GroupComboBox.SelectedItem?.ToString();
            if (selectedGroup == "-")
            {
                CustomerListBox.ItemsSource = _allCustomers;
            }
            else if (!string.IsNullOrWhiteSpace(selectedGroup))
            {
                var filteredCustomers = _allCustomers.Where(c => c.Group == selectedGroup).ToList();
                CustomerListBox.ItemsSource = filteredCustomers;
            }
            else
            {
                CustomerListBox.ItemsSource = _allCustomers;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();
            var filteredCustomers = _allCustomers
                .Where(c => c.FullName.ToLower().Contains(searchText))
                .ToList();

            string selectedGroup = GroupComboBox.SelectedItem?.ToString();
            if (selectedGroup != "-")
            {
                filteredCustomers = filteredCustomers.Where(c => c.Group == selectedGroup).ToList();
            }

            CustomerListBox.ItemsSource = filteredCustomers;
        }

        private void AddFundsButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerListBox.SelectedItem is Customer selectedCustomer)
            {
                if (decimal.TryParse(AmountTextBox.Text, out decimal amount))
                {
                    if (amount <= 0)
                    {
                        MessageBox.Show("Сума повинна бути більше нуля.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    selectedCustomer.Balance += amount;
                    SQLiteDataAccess.UpdateCustomer(selectedCustomer);

                    Logger.Log($"Внесено кошти: {amount:C} до балансу замовника {selectedCustomer.FullName}. Новий баланс: {selectedCustomer.Balance:C}");

                    BalanceTextBlock.Text = $"Баланс: {selectedCustomer.Balance:C}";
                    MessageBox.Show($"Успішно додано {amount:C} до балансу {selectedCustomer.FullName}.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);

                    PaymentCompleted?.Invoke();
                }
                else
                {
                    MessageBox.Show("Невірна сума. Будь ласка, введіть коректне значення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Будь ласка, оберіть замовника.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoPayButton_Click(object sender, RoutedEventArgs e)
        {
            if (CustomerListBox.SelectedItem is Customer selectedCustomer)
            {
                var orders = SQLiteDataAccess.GetOrders()
                    .Where(o => o.CustomerId == selectedCustomer.Id && o.Status == "Виконано/не оплачено")
                    .OrderBy(o => o.OrderDate)
                    .ToList();

                if (!orders.Any())
                {
                    MessageBox.Show("Для цього замовника немає виконаних, але не оплачених замовлень.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int paidOrdersCount = 0;
                var paidOrders = new List<Order>();

                foreach (var order in orders)
                {
                    if (selectedCustomer.Balance >= order.Price)
                    {
                        selectedCustomer.Balance -= order.Price;
                        order.Status = "Виконано і оплачено";
                        SQLiteDataAccess.UpdateOrder(order);
                        Logger.Log($"Автоматично оплачено замовлення: {order.Task.Category} - {order.Task.Name} для замовника {selectedCustomer.FullName}. Сума: {order.Price:C}. Новий баланс: {selectedCustomer.Balance:C}");
                        paidOrders.Add(order);
                        paidOrdersCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                SQLiteDataAccess.UpdateCustomer(selectedCustomer);
                BalanceTextBlock.Text = $"Баланс: {selectedCustomer.Balance:C}";

                if (paidOrdersCount == 0)
                {
                    Logger.Log($"Недостатньо коштів для оплати замовлень замовника {selectedCustomer.FullName}");
                    MessageBox.Show("Недостатньо коштів для оплати замовлень.", "Недостатньо коштів", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    Logger.Log($"Автооплата завершена для замовника {selectedCustomer.FullName}. Оплачено {paidOrdersCount} замовлень.");
                    MessageBox.Show($"Автооплата завершена. Оплачено {paidOrdersCount} замовлень.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                PaymentCompleted?.Invoke();
            }
            else
            {
                MessageBox.Show("Будь ласка, оберіть замовника.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}