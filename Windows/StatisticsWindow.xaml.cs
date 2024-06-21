using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;

namespace OrderManagerPlus.Windows
{
    public partial class StatisticsWindow : Window
    {
        public StatisticsWindow()
        {
            InitializeComponent();
            LoadStatistics();
            LoadLogs();
        }

        private void LoadStatistics()
        {
            var orders = SQLiteDataAccess.GetOrders();

            // Загальна кількість замовлень
            TotalOrdersTextBlock.Text = orders.Count.ToString();

            // Виконано, але не оплачено
            CompletedNotPaidTextBlock.Text = orders.Count(o => o.Status == "Виконано/не оплачено").ToString();

            // Виконано та оплачено
            CompletedPaidTextBlock.Text = orders.Count(o => o.Status == "Виконано і оплачено").ToString();

            // Загальний борг
            TotalDebtTextBlock.Text = orders.Where(o => o.Status == "Виконано/не оплачено").Sum(o => o.Price).ToString("C");

            // Всього не виконаних завдань
            NotCompletedTasksTextBlock.Text = orders.Count(o => o.Status == "Не виконано").ToString();

            // Частково виконаних завдань
            PartiallyCompletedTasksTextBlock.Text = orders.Count(o => o.Status == "Частково виконано").ToString();
        }

        private void LoadLogs()
        {
            try
            {
                LogTextBox.Text = File.ReadAllText("log.txt");
            }
            catch (Exception ex)
            {
                LogTextBox.Text = $"Не вдалося завантажити лог: {ex.Message}";
            }
        }

        private void ListSwitcherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSwitcherComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "Баланс замовників":
                        LoadCustomerBalances();
                        break;
                    case "Борг замовників":
                        LoadCustomerDebts();
                        break;
                    case "Всього до сплати замовниками":
                        LoadCustomerTotalToPay();
                        break;
                    case "Всього оплачено замовниками":
                        LoadCustomerTotalPaid();
                        break;
                }
            }
        }

        private void LoadCustomerBalances()
        {
            var customers = SQLiteDataAccess.GetCustomers();
            var balances = customers.Select(c => new { c.FullName, c.Balance }).ToList();
            StatisticsListBox.ItemsSource = balances.Select(b => $"{b.FullName}: {b.Balance:C}");
        }

        private void LoadCustomerDebts()
        {
            var orders = SQLiteDataAccess.GetOrders()
                .Where(o => o.Status == "Виконано/не оплачено")
                .GroupBy(o => o.CustomerId)
                .Select(g => new
                {
                    Customer = SQLiteDataAccess.GetCustomers().FirstOrDefault(c => c.Id == g.Key),
                    Debt = g.Sum(o => o.Price)
                })
                .Where(d => d.Debt > 0)
                .ToList();

            StatisticsListBox.ItemsSource = orders.Select(d => $"{d.Customer?.FullName ?? "Невідомий"}: {d.Debt:C}");
        }

        private void LoadCustomerTotalToPay()
        {
            var customers = SQLiteDataAccess.GetCustomers();
            var orders = SQLiteDataAccess.GetOrders();

            var totalToPay = customers.Select(c => new
            {
                c.FullName,
                TotalToPay = orders.Where(o => o.CustomerId == c.Id).Sum(o => o.Price)
            }).ToList();

            StatisticsListBox.ItemsSource = totalToPay.Select(t => $"{t.FullName}: {t.TotalToPay:C}");
        }

        private void LoadCustomerTotalPaid()
        {
            var customers = SQLiteDataAccess.GetCustomers();
            var orders = SQLiteDataAccess.GetOrders();

            var totalPaid = customers.Select(c => new
            {
                c.FullName,
                TotalPaid = orders.Where(o => o.CustomerId == c.Id && o.Status == "Виконано і оплачено").Sum(o => o.Price)
            }).ToList();

            StatisticsListBox.ItemsSource = totalPaid.Select(t => $"{t.FullName}: {t.TotalPaid:C}");
        }
    }
}