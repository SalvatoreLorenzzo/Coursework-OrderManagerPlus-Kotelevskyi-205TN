using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Data;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using System.ComponentModel;
using OrderManagerPlus.Logging;
using System.IO;
using Newtonsoft.Json;

namespace OrderManagerPlus.Windows
{
    public partial class MainWindow : Window
    {
        private bool isMenuVisible = false;
        private List<Order> orders;
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private Order _orderToDelete;
        private string _deleteSource;

        public MainWindow()
        {
            InitializeComponent();
            LoadDatabasePath();
            EnsureDatabaseCreated();
            LoadData();
        }

        private void EnsureDatabaseCreated()
        {
            try
            {
                using (var context = new Context())
                {
                    if (!context.Database.CanConnect())
                    {
                        context.Database.EnsureCreated();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка створення бази даних: {ex.Message}");
            }
        }

        public void ReloadOrders()
        {
            orders = SQLiteDataAccess.GetOrders();
            OrdersListView.ItemsSource = orders;

            InitializeTable();
            ApplyListFilters();
            ApplyTableFilters();
        }

        public void LoadDatabasePath()
        {
            AppSettings settings = AppSettings.Load();
            if (!string.IsNullOrEmpty(settings.DatabasePath) && File.Exists(settings.DatabasePath))
            {
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetDirectoryName(settings.DatabasePath));
            }
            else
            {
                settings.DatabasePath = "ordermanagerplus.db";
                settings.Save();
                AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetDirectoryName(settings.DatabasePath));
            }
        }

        private void ToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            Storyboard sb = isMenuVisible ? (Storyboard)FindResource("CollapseMenu") : (Storyboard)FindResource("ExpandMenu");
            sb.Begin();
            isMenuVisible = !isMenuVisible;
        }

        private void LoadData()
        {
            orders = SQLiteDataAccess.GetOrders();
            OrdersListView.ItemsSource = orders;
            ApplyListFilters();
            InitializeTable();
            ApplyTableFilters();
            UpdateRemoveFilterButtonVisibility();
        }

        private void InitializeTable()
        {
            var customers = SQLiteDataAccess.GetCustomers().OrderBy(c => c.FullName).ToList();
            var tasks = SQLiteDataAccess.GetTasks().OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();

            OrdersDataGrid.Columns.Clear();
            OrdersDataGrid.Columns.Add(new DataGridTextColumn { Header = "Група", Binding = new Binding("Group") });
            OrdersDataGrid.Columns.Add(new DataGridTextColumn { Header = "Замовник", Binding = new Binding("Customer") });

            foreach (var task in tasks)
            {
                OrdersDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = task.Category + "\n" + task.Name,
                    Binding = new Binding($"TaskStatuses[{task.Name}]")
                });
            }

            UpdateTableData(customers, tasks, orders);
        }

        private void UpdateTableData(IEnumerable<Customer> customers, IEnumerable<Task> tasks, IEnumerable<Order> orders)
        {
            var tableDataItems = new List<TableDataItem>();
            foreach (var customer in customers)
            {
                var row = new TableDataItem
                {
                    Group = customer.Group,
                    Customer = customer.FullName,
                    TaskStatuses = new Dictionary<string, string>()
                };

                foreach (var task in tasks)
                {
                    var key = $"{task.Category} - {task.Name}";
                    var order = orders.FirstOrDefault(o => o.CustomerId == customer.Id && o.TaskId == task.Id);
                    if (order != null)
                    {
                        row.TaskStatuses[key] = order.Status;
                    }
                    else
                    {
                        row.TaskStatuses[key] = "Не замовлено";
                    }
                }

                tableDataItems.Add(row);
            }

            OrdersDataGrid.ItemsSource = tableDataItems;
        }

        private void AddCustomer_Click(object sender, RoutedEventArgs e)
        {
            var addCustomerWindow = new AddCustomerWindow();
            addCustomerWindow.ShowDialog();
            LoadData();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var addTaskWindow = new AddTaskWindow();
            addTaskWindow.ShowDialog();
            LoadData();
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            var addOrderWindow = new AddOrderWindow();
            addOrderWindow.ShowDialog();
            LoadData();
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void Statistics_Click(object sender, RoutedEventArgs e)
        {
            var statisticsWindow = new StatisticsWindow();
            statisticsWindow.ShowDialog();
        }

        private void Payment_Click(object sender, RoutedEventArgs e)
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.PaymentCompleted += LoadData; // Підписка на подію
            paymentWindow.ShowDialog();
        }

        private void AddListViewFilter_Click(object sender, RoutedEventArgs e)
        {
            var filterPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            var filterTypeComboBox = new ComboBox { Width = 150 };
            filterTypeComboBox.Items.Add("За групою");
            filterTypeComboBox.Items.Add("За сферою");
            filterTypeComboBox.Items.Add("За статусом");
            filterTypeComboBox.SelectionChanged += (s, ev) => LoadFilterDetails(filterTypeComboBox, filterPanel);
            filterPanel.Children.Add(filterTypeComboBox);

            ListSettingsPanel.Children.Add(filterPanel);

            UpdateRemoveFilterButtonVisibility();
        }

        private void AddTableViewFilter_Click(object sender, RoutedEventArgs e)
        {
            var filterPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            var filterTypeComboBox = new ComboBox { Width = 150 };
            filterTypeComboBox.Items.Add("За групою");
            filterTypeComboBox.Items.Add("За сферою");
            filterTypeComboBox.SelectionChanged += (s, ev) => LoadTableFilterDetails(filterTypeComboBox, filterPanel);
            filterPanel.Children.Add(filterTypeComboBox);

            TableSettingsPanel.Children.Add(filterPanel);

            UpdateRemoveFilterButtonVisibility();
        }

        private void LoadFilterDetails(ComboBox filterTypeComboBox, StackPanel filterPanel)
        {
            while (filterPanel.Children.Count > 1)
            {
                filterPanel.Children.RemoveAt(1);
            }

            if (filterTypeComboBox.SelectedItem != null)
            {
                var filterType = filterTypeComboBox.SelectedItem.ToString();
                var filterComboBox = new ComboBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };

                if (filterType == "За групою")
                {
                    var groups = SQLiteDataAccess.GetCustomers().Select(c => c.Group).Distinct().ToList();
                    groups.Insert(0, "-");
                    filterComboBox.ItemsSource = groups;
                }
                else if (filterType == "За сферою")
                {
                    var categories = SQLiteDataAccess.GetTasks().Select(t => t.Category).Distinct().ToList();
                    categories.Insert(0, "-");
                    filterComboBox.ItemsSource = categories;
                }
                else if (filterType == "За статусом") // Додано новий тип сортування
                {
                    filterComboBox.Items.Add("Всі");
                    filterComboBox.Items.Add("Всі окрім");
                    filterComboBox.SelectionChanged += (s, ev) => LoadSecondaryFilterDetails(filterComboBox, filterType, filterPanel);
                }

                filterComboBox.SelectionChanged += (s, ev) => LoadSecondaryFilterDetails(filterComboBox, filterType, filterPanel);
                filterPanel.Children.Add(filterComboBox);
            }
        }

        private void LoadStatusFilterDetails(ComboBox filterComboBox, StackPanel filterPanel)
        {
            while (filterPanel.Children.Count > 2)
            {
                filterPanel.Children.RemoveAt(2);
            }

            if (filterComboBox.SelectedItem != null)
            {
                var statusComboBox = new ComboBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };
                var statuses = new List<string> { "Не виконано", "Частково виконано", "Виконано/не оплачено", "Виконано і оплачено" };

                statusComboBox.ItemsSource = statuses;

                filterPanel.Children.Add(statusComboBox);
                statusComboBox.SelectionChanged += (s, ev) => ApplyListFilters();
                ApplyListFilters();
            }
        }

        private void LoadSecondaryFilterDetails(ComboBox filterComboBox, string filterType, StackPanel filterPanel)
        {
            while (filterPanel.Children.Count > 2)
            {
                filterPanel.Children.RemoveAt(2);
            }

            if (filterComboBox.SelectedItem != null)
            {
                var secondaryFilterComboBox = new ComboBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };

                if (filterType == "За групою")
                {
                    var selectedGroup = filterComboBox.SelectedItem.ToString();
                    var customers = SQLiteDataAccess.GetCustomers().Where(c => c.Group == selectedGroup).ToList();
                    customers.Insert(0, new Customer { FullName = "-" });
                    secondaryFilterComboBox.ItemsSource = customers;
                    secondaryFilterComboBox.DisplayMemberPath = "FullName";
                }
                else if (filterType == "За сферою")
                {
                    var selectedCategory = filterComboBox.SelectedItem.ToString();
                    var tasks = SQLiteDataAccess.GetTasks().Where(t => t.Category == selectedCategory).ToList();
                    tasks.Insert(0, new Task { Name = "-" });
                    secondaryFilterComboBox.ItemsSource = tasks;
                    secondaryFilterComboBox.DisplayMemberPath = "Name";
                }
                else if (filterType == "За статусом") // Додано новий тип сортування
                {
                    var statuses = new List<string> { "-", "Не виконано", "Частково виконано", "Виконано/не оплачено", "Виконано і оплачено" };
                    secondaryFilterComboBox.ItemsSource = statuses;
                }

                secondaryFilterComboBox.SelectionChanged += (s, ev) => ApplyListFilters();
                secondaryFilterComboBox.SelectedIndex = 0;

                if (!filterPanel.Children.OfType<Button>().Any())
                {
                    var deleteButton = new Button { Content = "Delete", Margin = new Thickness(5, 0, 0, 0) };
                    deleteButton.Click += (s, ev) => {
                        ListSettingsPanel.Children.Remove(filterPanel);
                        ApplyListFilters();
                    };

                    filterPanel.Children.Add(secondaryFilterComboBox);
                    filterPanel.Children.Add(deleteButton);
                }
                else
                {
                    filterPanel.Children.Insert(2, secondaryFilterComboBox);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(OrdersListView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                    var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                    Sort(sortBy, direction);

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void LoadTableFilterDetails(ComboBox filterTypeComboBox, StackPanel filterPanel)
        {
            // Очищення дочірніх елементів у панелі, крім першого (ComboBox з типом фільтра)
            while (filterPanel.Children.Count > 1)
            {
                filterPanel.Children.RemoveAt(1);
            }

            if (filterTypeComboBox.SelectedItem != null)
            {
                var filterType = filterTypeComboBox.SelectedItem.ToString();
                var filterComboBox = new ComboBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };

                if (filterType == "За групою")
                {
                    var groups = SQLiteDataAccess.GetCustomers().Select(c => c.Group).Distinct().ToList();
                    groups.Insert(0, "-");
                    filterComboBox.ItemsSource = groups;
                }
                else if (filterType == "За сферою")
                {
                    var categories = SQLiteDataAccess.GetTasks().Select(t => t.Category).Distinct().ToList();
                    categories.Insert(0, "-");
                    filterComboBox.ItemsSource = categories;
                }

                filterComboBox.SelectionChanged += (s, ev) => LoadSecondaryTableFilterDetails(filterComboBox, filterType, filterPanel);
                filterPanel.Children.Add(filterComboBox);
            }
        }

        private void LoadSecondaryTableFilterDetails(ComboBox filterComboBox, string filterType, StackPanel filterPanel)
        {
            while (filterPanel.Children.Count > 2)
            {
                filterPanel.Children.RemoveAt(2);
            }

            if (filterComboBox.SelectedItem != null)
            {
                var secondaryFilterComboBox = new ComboBox { Width = 150, Margin = new Thickness(5, 0, 0, 0) };

                if (filterType == "За групою")
                {
                    var selectedGroup = filterComboBox.SelectedItem.ToString();
                    var customers = SQLiteDataAccess.GetCustomers().Where(c => c.Group == selectedGroup).ToList();
                    customers.Insert(0, new Customer { FullName = "-" });
                    secondaryFilterComboBox.ItemsSource = customers;
                    secondaryFilterComboBox.DisplayMemberPath = "FullName";
                }
                else if (filterType == "За сферою")
                {
                    var selectedCategory = filterComboBox.SelectedItem.ToString();
                    var tasks = SQLiteDataAccess.GetTasks().Where(t => t.Category == selectedCategory).ToList();
                    tasks.Insert(0, new Task { Name = "-" });
                    secondaryFilterComboBox.ItemsSource = tasks;
                    secondaryFilterComboBox.DisplayMemberPath = "Name";
                }

                secondaryFilterComboBox.SelectionChanged += (s, ev) => ApplyTableFilters();
                secondaryFilterComboBox.SelectedIndex = 0;

                if (!filterPanel.Children.OfType<Button>().Any())
                {
                    var deleteButton = new Button { Content = "Delete", Margin = new Thickness(5, 0, 0, 0) };
                    deleteButton.Click += (s, ev) => {
                        TableSettingsPanel.Children.Remove(filterPanel);
                        ApplyTableFilters();
                    };

                    filterPanel.Children.Add(secondaryFilterComboBox);
                    filterPanel.Children.Add(deleteButton);
                }
                else
                {
                    filterPanel.Children.Insert(2, secondaryFilterComboBox);
                }
            }
        }

        private void ApplyListFilters()
        {
            var filteredOrders = orders.AsQueryable();
            var groupFilters = new List<(string Group, int? CustomerId)>();
            var categoryFilters = new List<(string Category, int? TaskId)>();
            var statusFilters = new List<(string FilterType, string Status)>();

            foreach (var child in ListSettingsPanel.Children)
            {
                if (child is StackPanel filterPanel)
                {
                    var filterComboBox = filterPanel.Children.OfType<ComboBox>().FirstOrDefault();
                    if (filterComboBox != null)
                    {
                        var filterType = filterComboBox.SelectionBoxItem.ToString();
                        var secondaryFilterComboBox = filterPanel.Children.OfType<ComboBox>().ElementAtOrDefault(1);
                        var tertiaryFilterComboBox = filterPanel.Children.OfType<ComboBox>().ElementAtOrDefault(2);

                        if (filterType == "За групою" && secondaryFilterComboBox != null)
                        {
                            var selectedGroup = secondaryFilterComboBox.SelectedItem.ToString();
                            if (selectedGroup != "-")
                            {
                                int? selectedCustomerId = null;
                                if (tertiaryFilterComboBox != null && tertiaryFilterComboBox.SelectedItem is Customer selectedCustomer && selectedCustomer.FullName != "-")
                                {
                                    selectedCustomerId = selectedCustomer.Id;
                                }
                                groupFilters.Add((selectedGroup, selectedCustomerId));
                            }
                        }
                        else if (filterType == "За сферою" && secondaryFilterComboBox != null)
                        {
                            var selectedCategory = secondaryFilterComboBox.SelectedItem.ToString();
                            if (selectedCategory != "-")
                            {
                                int? selectedTaskId = null;
                                if (tertiaryFilterComboBox != null && tertiaryFilterComboBox.SelectedItem is Task selectedTask && selectedTask.Name != "-")
                                {
                                    selectedTaskId = selectedTask.Id;
                                }
                                categoryFilters.Add((selectedCategory, selectedTaskId));
                            }
                        }
                        else if (filterType == "За статусом" && secondaryFilterComboBox != null)
                        {
                            var filterOption = secondaryFilterComboBox.SelectedItem.ToString();
                            var selectedStatus = tertiaryFilterComboBox?.SelectedItem?.ToString();
                            if (selectedStatus != null && selectedStatus != "-")
                            {
                                statusFilters.Add((filterOption, selectedStatus));
                            }
                        }
                    }
                }
            }

            if (groupFilters.Any())
            {
                foreach (var (group, customerId) in groupFilters)
                {
                    filteredOrders = filteredOrders.Where(o => o.Customer.Group == group &&
                                                               (!customerId.HasValue || o.CustomerId == customerId.Value));
                }
            }

            if (categoryFilters.Any())
            {
                foreach (var (category, taskId) in categoryFilters)
                {
                    filteredOrders = filteredOrders.Where(o => o.Task.Category == category &&
                                                               (!taskId.HasValue || o.TaskId == taskId.Value));
                }
            }

            if (statusFilters.Any())
            {
                foreach (var (filterOption, status) in statusFilters)
                {
                    if (filterOption == "Всі окрім")
                    {
                        filteredOrders = filteredOrders.Where(o => o.Status != status);
                    }
                    else
                    {
                        filteredOrders = filteredOrders.Where(o => o.Status == status);
                    }
                }
            }

            OrdersListView.ItemsSource = filteredOrders.ToList();
        }

        private void ApplyTableFilters()
        {
            var customers = SQLiteDataAccess.GetCustomers().OrderBy(c => c.FullName).ToList();
            var tasks = SQLiteDataAccess.GetTasks().OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();

            var filteredCustomers = customers.AsQueryable();
            var filteredOrders = orders.AsQueryable();
            var filteredTasks = tasks.AsQueryable();

            foreach (var child in TableSettingsPanel.Children)
            {
                if (child is StackPanel filterPanel)
                {
                    var filterComboBox = filterPanel.Children.OfType<ComboBox>().FirstOrDefault();
                    if (filterComboBox != null)
                    {
                        var filterType = filterComboBox.SelectionBoxItem.ToString();
                        var secondaryFilterComboBox = filterPanel.Children.OfType<ComboBox>().ElementAtOrDefault(1);
                        var tertiaryFilterComboBox = filterPanel.Children.OfType<ComboBox>().ElementAtOrDefault(2);

                        if (filterType == "За групою" && secondaryFilterComboBox != null)
                        {
                            var selectedGroup = secondaryFilterComboBox.SelectedItem.ToString();
                            if (selectedGroup != "-")
                            {
                                int? selectedCustomerId = null;
                                if (tertiaryFilterComboBox != null && tertiaryFilterComboBox.SelectedItem is Customer selectedCustomer && selectedCustomer.FullName != "-")
                                {
                                    selectedCustomerId = selectedCustomer.Id;
                                }
                                filteredCustomers = filteredCustomers.Where(c => c.Group == selectedGroup &&
                                                                                 (!selectedCustomerId.HasValue || c.Id == selectedCustomerId.Value));
                            }
                        }
                        else if (filterType == "За сферою" && secondaryFilterComboBox != null)
                        {
                            var selectedCategory = secondaryFilterComboBox.SelectedItem.ToString();
                            if (selectedCategory != "-")
                            {
                                int? selectedTaskId = null;
                                if (tertiaryFilterComboBox != null && tertiaryFilterComboBox.SelectedItem is Task selectedTask && selectedTask.Name != "-")
                                {
                                    selectedTaskId = selectedTask.Id;
                                }
                                filteredTasks = filteredTasks.Where(t => t.Category == selectedCategory &&
                                                                         (!selectedTaskId.HasValue || t.Id == selectedTaskId.Value));
                            }
                        }
                    }
                }
            }

            if (OnlyOrderedCheckBox.IsChecked == true)
            {
                // Фільтрування замовників, які мають замовлення
                filteredCustomers = filteredCustomers.Where(c => orders.Any(o => o.CustomerId == c.Id));

                // Фільтрування завдань, які були замовлені
                filteredTasks = filteredTasks.Where(t => orders.Any(o => o.TaskId == t.Id));
            }

            var tableDataItems = new List<TableDataItem>();
            foreach (var customer in filteredCustomers)
            {
                var row = new TableDataItem
                {
                    Group = customer.Group,
                    Customer = customer.FullName,
                    TaskStatuses = new Dictionary<string, string>()
                };

                foreach (var task in filteredTasks)
                {
                    var key = $"{task.Category} - {task.Name}";
                    var order = filteredOrders.FirstOrDefault(o => o.CustomerId == customer.Id && o.TaskId == task.Id);
                    if (order != null)
                    {
                        row.TaskStatuses[key] = order.Status;
                    }
                    else
                    {
                        row.TaskStatuses[key] = "Не замовлено";
                    }
                }

                tableDataItems.Add(row);
            }

            OrdersDataGrid.ItemsSource = tableDataItems;

            // Видалення зайвих стовпців
            var columnsToRemove = OrdersDataGrid.Columns.Skip(2).ToList();
            foreach (var column in columnsToRemove)
            {
                OrdersDataGrid.Columns.Remove(column);
            }

            // Додавання стовпців для відфільтрованих завдань
            foreach (var task in filteredTasks)
            {
                OrdersDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"{task.Category}\n{task.Name}",
                    Binding = new Binding($"TaskStatuses[{task.Category} - {task.Name}]")
                });
            }
        }

        private void OnlyOrderedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTableFilters();
        }

        private void OnlyOrderedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyTableFilters();
        }

        private void OrdersDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var column = e.Column;
            var direction = column.SortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            column.SortDirection = direction;

            var sortBy = column.Header.ToString();
            var tableDataItems = OrdersDataGrid.ItemsSource as List<TableDataItem>;

            if (tableDataItems != null)
            {
                if (sortBy == "Група")
                {
                    if (direction == ListSortDirection.Ascending)
                    {
                        tableDataItems = tableDataItems.OrderBy(x => x.Group).ToList();
                    }
                    else
                    {
                        tableDataItems = tableDataItems.OrderByDescending(x => x.Group).ToList();
                    }
                }
                else if (sortBy == "Замовник")
                {
                    if (direction == ListSortDirection.Ascending)
                    {
                        tableDataItems = tableDataItems.OrderBy(x => x.Customer).ToList();
                    }
                    else
                    {
                        tableDataItems = tableDataItems.OrderByDescending(x => x.Customer).ToList();
                    }
                }
                else
                {
                    var taskName = sortBy.Split('\n').Last();
                    if (direction == ListSortDirection.Ascending)
                    {
                        tableDataItems = tableDataItems.OrderBy(x => x.TaskStatuses.ContainsKey(taskName) ? x.TaskStatuses[taskName] : string.Empty).ToList();
                    }
                    else
                    {
                        tableDataItems = tableDataItems.OrderByDescending(x => x.TaskStatuses.ContainsKey(taskName) ? x.TaskStatuses[taskName] : string.Empty).ToList();
                    }
                }

                OrdersDataGrid.ItemsSource = tableDataItems;
            }

            e.Handled = true;
        }

        private void UpdateRemoveFilterButtonVisibility()
        {
            RemoveListViewFilterButton.Visibility = ListSettingsPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            RemoveTableViewFilterButton.Visibility = TableSettingsPanel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RemoveLastListViewFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ListSettingsPanel.Children.Count > 0)
            {
                ListSettingsPanel.Children.RemoveAt(ListSettingsPanel.Children.Count - 1);
                ApplyListFilters();
            }

            UpdateRemoveFilterButtonVisibility();
        }

        private void RemoveLastTableViewFilter_Click(object sender, RoutedEventArgs e)
        {
            if (TableSettingsPanel.Children.Count > 0)
            {
                TableSettingsPanel.Children.RemoveAt(TableSettingsPanel.Children.Count - 1);
                ApplyTableFilters();
            }

            UpdateRemoveFilterButtonVisibility();
        }

        private void OrdersListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrdersListView.SelectedItem is Order selectedOrder)
            {
                var editOrderWindow = new EditOrderWindow(selectedOrder);
                editOrderWindow.OrderUpdated += LoadData; // Підписка на подію
                editOrderWindow.ShowDialog();
                LoadData();
            }
        }

        private void OrdersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is TableDataItem selectedItem)
            {
                var customerName = selectedItem.Customer;
                var taskName = OrdersDataGrid.CurrentColumn.Header.ToString().Split('\n').Last();

                var order = orders.FirstOrDefault(o => o.Customer.FullName == customerName && o.Task.Name == taskName);
                if (order != null)
                {
                    var editOrderWindow = new EditOrderWindow(order);
                    editOrderWindow.OrderUpdated += LoadData; // Підписка на подію
                    editOrderWindow.ShowDialog();
                    LoadData();
                }
            }
        }

        private void SetStatusNotCompleted_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetOrderStatus("Не виконано");
        }

        private void SetStatusPartiallyCompleted_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetOrderStatus("Частково виконано");
        }

        private void SetStatusNotPaid_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetOrderStatus("Виконано/не оплачено");
        }

        private void SetStatusPaid_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SetOrderStatus("Виконано і оплачено");
        }

        private void SetStatus_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = OrdersListView.SelectedItem != null || OrdersDataGrid.SelectedItem != null;
        }

        private void ToggleMenu_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleMenu_Click(sender, e);
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                await System.Threading.Tasks.Task.Delay(200); // Затримка для уникнення зламу анімації
                ToggleMenu_Click(sender, null);
            }
            else if (e.Key == Key.Delete)
            {
                PromptDeleteOrder();
            }
            else if ((e.Key == Key.D1 || e.Key == Key.NumPad1) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetOrderStatus("Не виконано", false);
            }
            else if ((e.Key == Key.D2 || e.Key == Key.NumPad2) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetOrderStatus("Частково виконано", false);
            }
            else if ((e.Key == Key.D3 || e.Key == Key.NumPad3) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetOrderStatus("Виконано/не оплачено", false);
            }
            else if ((e.Key == Key.D4 || e.Key == Key.NumPad4) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SetOrderStatus("Виконано і оплачено", false);
            }
        }

        private void PromptDeleteOrder()
        {
            if (OrdersListView.SelectedItem is Order selectedOrder)
            {
                _orderToDelete = selectedOrder;
                _deleteSource = "ListView";
                MessageBoxResult result = MessageBox.Show("Ви впевнені, що хочете видалити це замовлення? Натисніть Enter для підтвердження або Esc для скасування.", "Підтвердження видалення", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    ConfirmDeleteOrder();
                }
                else
                {
                    CancelDeleteOrder();
                }
            }
            else if (OrdersDataGrid.SelectedItem is TableDataItem selectedItem && OrdersDataGrid.CurrentColumn != null)
            {
                var customerName = selectedItem.Customer;
                var taskName = OrdersDataGrid.CurrentColumn.Header.ToString().Split('\n').Last();
                var order = orders.FirstOrDefault(o => o.Customer.FullName == customerName && o.Task.Name == taskName);

                if (order != null)
                {
                    _orderToDelete = order;
                    _deleteSource = "DataGrid";
                    MessageBoxResult result = MessageBox.Show("Ви впевнені, що хочете видалити це замовлення? Натисніть Enter для підтвердження або Esc для скасування.", "Підтвердження видалення", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.OK)
                    {
                        ConfirmDeleteOrder();
                    }
                    else
                    {
                        CancelDeleteOrder();
                    }
                }
                else
                {
                    MessageBox.Show("Неможливо видалити, оскільки завдання не було замовлено.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddCustomer_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addCustomerWindow = new AddCustomerWindow();
            addCustomerWindow.ShowDialog();
            LoadData();
        }

        private void AddTask_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addTaskWindow = new AddTaskWindow();
            addTaskWindow.ShowDialog();
            LoadData();
        }

        private void AddOrder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var addOrderWindow = new AddOrderWindow();
            addOrderWindow.ShowDialog();
            LoadData();
        }

        private void Settings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void Statistics_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var statisticsWindow = new StatisticsWindow();
            statisticsWindow.ShowDialog();
        }

        private void Payment_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var paymentWindow = new PaymentWindow();
            paymentWindow.PaymentCompleted += LoadData; // Підписка на подію
            paymentWindow.ShowDialog();
        }

        private void ConfirmDeleteOrder()
        {
            try
            {
                if (_orderToDelete != null)
                {
                    SQLiteDataAccess.DeleteOrder(_orderToDelete.Id);
                    Logger.Log($"Замовлення {_orderToDelete.Task.Category} - {_orderToDelete.Task.Name} для замовника {_orderToDelete.Customer.FullName} було видалено.");
                    _orderToDelete = null;
                    _deleteSource = null;
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при видаленні замовлення: {ex.Message}");
                MessageBox.Show($"Сталася помилка при видаленні замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelDeleteOrder()
        {
            _orderToDelete = null;
            _deleteSource = null;
        }

        private void SetOrderStatus(string status, bool showMessage = true)
        {
            try
            {
                if (OrdersListView.SelectedItem is Order selectedOrder)
                {
                    selectedOrder.Status = status;
                    SQLiteDataAccess.UpdateOrder(selectedOrder);
                    Logger.Log($"Статус замовлення змінено: {selectedOrder.Task.Category} - {selectedOrder.Task.Name} для замовника {selectedOrder.Customer.FullName}. Новий статус: {status}, змінено хоткеями");
                    LoadData();
                    OrdersListView.SelectedItem = selectedOrder; // Повернути виділення
                }
                else if (OrdersDataGrid.SelectedItem is TableDataItem selectedItem && OrdersDataGrid.CurrentColumn != null)
                {
                    var customerName = selectedItem.Customer;
                    var taskName = OrdersDataGrid.CurrentColumn.Header.ToString().Split('\n').Last();
                    var order = orders.FirstOrDefault(o => o.Customer.FullName == customerName && o.Task.Name == taskName);

                    if (order != null)
                    {
                        order.Status = status;
                        SQLiteDataAccess.UpdateOrder(order);
                        Logger.Log($"Статус замовлення змінено: {order.Task.Category} - {order.Task.Name} для замовника {order.Customer.FullName}. Новий статус: {status}, змінено хоткеями");
                        LoadData();
                        OrdersDataGrid.SelectedItem = selectedItem; // Повернути виділення
                    }
                    else
                    {
                        MessageBox.Show($"Конкретне завдання '{taskName}' не було створено для даного замовника '{customerName}'.", "Завдання не знайдено", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при зміні статусу замовлення: {ex.Message}");
                MessageBox.Show($"Сталася помилка при зміні статусу замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class TableDataItem
        {
            public string Group { get; set; }
            public string Customer { get; set; }
            public Dictionary<string, string> TaskStatuses { get; set; } = new Dictionary<string, string>();
        }
    }
}