using System;
using System.Linq;
using System.Windows;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;
using System.Windows.Media.Animation;

namespace OrderManagerPlus.Windows
{
    public partial class AddOrderWindow : Window
    {
        private Customer _initialCustomer;
        private Task _initialTask;
        private bool _isInitializedFromTable;
        private bool isMenuVisible = false;

        public AddOrderWindow()
        {
            InitializeComponent();
            this.Loaded += AddOrderWindow_Loaded;
        }

        private void AddOrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            LoadCategories();
            OrderDateTextBox.Text = DateTime.Now.ToString("yyyy-MM-dd");

            if (_initialCustomer != null && _initialTask != null)
            {
                SetInitialData();
            }
        }

        private void LoadGroups()
        {
            var groups = SQLiteDataAccess.GetCustomers().Select(c => c.Group).Distinct().ToList();
            GroupComboBox.ItemsSource = groups;
        }

        private void LoadCustomersByGroup(string group)
        {
            var customers = SQLiteDataAccess.GetCustomers().Where(c => c.Group == group).OrderBy(c => c.FullName).ToList();
            CustomerComboBox.ItemsSource = customers;
            CustomerComboBox.DisplayMemberPath = "FullName";
        }

        private void LoadCategories()
        {
            var categories = SQLiteDataAccess.GetTasks().Select(t => t.Category).Distinct().ToList();
            CategoryComboBox.ItemsSource = categories;
        }

        private void LoadTasks(string category)
        {
            var tasks = SQLiteDataAccess.GetTasks().Where(t => t.Category == category).OrderBy(t => t.Name).ToList();
            TaskComboBox.ItemsSource = tasks;
            TaskComboBox.DisplayMemberPath = "Name";
        }

        private void GroupComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GroupComboBox.SelectedItem != null)
            {
                LoadCustomersByGroup(GroupComboBox.SelectedItem.ToString());
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem != null)
            {
                LoadTasks(CategoryComboBox.SelectedItem.ToString());
            }
        }

        private void TaskComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedTask = (Task)TaskComboBox.SelectedItem;
            if (selectedTask != null)
            {
                PriceTextBox.Text = selectedTask.Price.ToString();
            }
        }

        public void LoadInitialData(Customer initialCustomer, Task initialTask, bool isInitializedFromTable)
        {
            _initialCustomer = initialCustomer;
            _initialTask = initialTask;
            _isInitializedFromTable = isInitializedFromTable;
        }

        private void SetInitialData()
        {
            if (_isInitializedFromTable)
            {
                // Заповнюємо групу та сферу
                GroupComboBox.SelectedItem = _initialCustomer.Group;
                CategoryComboBox.SelectedItem = _initialTask.Category;

                // Завантажуємо замовників для групи
                LoadCustomersByGroup(_initialCustomer.Group);
                // Завантажуємо завдання для сфери
                LoadTasks(_initialTask.Category);

                // Встановлюємо вибраного замовника та завдання після їх завантаження
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CustomerComboBox.SelectedItem = _initialCustomer;
                    TaskComboBox.SelectedItem = _initialTask;

                    // Заблокуємо вибрані поля
                    GroupComboBox.IsEnabled = false;
                    CustomerComboBox.IsEnabled = false;
                    CategoryComboBox.IsEnabled = false;
                    TaskComboBox.IsEnabled = false;
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
            else
            {
                // Якщо вікно відкрите не через таблицю, залишимо всі поля доступними для редагування
                GroupComboBox.IsEnabled = true;
                CustomerComboBox.IsEnabled = true;
                CategoryComboBox.IsEnabled = true;
                TaskComboBox.IsEnabled = true;
            }

            // Заповнюємо інші поля
            OrderDateTextBox.Text = DateTime.Now.ToString("yyyy-MM-dd");
            if (_initialTask != null)
            {
                PriceTextBox.Text = _initialTask.Price.ToString();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCustomer = (Customer)CustomerComboBox.SelectedItem;
            var selectedTask = (Task)TaskComboBox.SelectedItem;

            if (selectedCustomer == null || selectedTask == null)
            {
                MessageBox.Show("Будь ласка, оберіть замовника та завдання.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SQLiteDataAccess.GetOrders().Any(o => o.CustomerId == selectedCustomer.Id && o.TaskId == selectedTask.Id))
            {
                MessageBox.Show("Це завдання вже було замовлено цим замовником.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime orderDate = DateTime.Now;
            DateTime? dueDate = null;

            if (DateTime.TryParse(DueDateTextBox.Text, out DateTime parsedDueDate))
            {
                dueDate = parsedDueDate;
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

            if (price == 0)
            {
                MessageBoxResult result = MessageBox.Show("Ціна дорівнює 0. Ви впевнені, що хочете додати це замовлення?", "Попередження", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            if (!decimal.TryParse(DiscountTextBox.Text, out decimal discount))
            {
                MessageBox.Show("Невірна знижка.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (PercentageDiscountRadioButton.IsChecked == true)
            {
                if (discount < 0 || discount > 100)
                {
                    MessageBox.Show("Знижка у відсотках повинна бути між 0 і 100.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                discount = price * (discount / 100);
            }
            else if (AmountDiscountRadioButton.IsChecked == true)
            {
                if (discount < 0)
                {
                    MessageBox.Show("Знижка у сумі не може бути від'ємною.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (discount > price)
                {
                    MessageBox.Show("Знижка у сумі не може бути більшою за ціну.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Order newOrder = new Order
            {
                CustomerId = selectedCustomer.Id,
                TaskId = selectedTask.Id,
                OrderDate = orderDate,
                DueDate = dueDate,
                Price = price - discount,
                Discount = discount,
                DiscountType = PercentageDiscountRadioButton.IsChecked == true ? "Відсоток" : "Сума",
                Status = "Не виконано"
            };

            try
            {
                SQLiteDataAccess.AddOrder(newOrder);
                Logger.Log($"Додано нове замовлення: {selectedTask.Name} для замовника {selectedCustomer.FullName} з ціною {newOrder.Price} та статусом {newOrder.Status}");
                MessageBox.Show("Замовлення успішно додано.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Закрити вікно та повернути результат
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при додаванні нового замовлення: {ex.Message}");
                MessageBox.Show($"Сталася помилка: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}