using System;
using System.Linq;
using System.Windows;
using OrderManagerPlus.Models;
using OrderManagerPlus.DataAccess;
using OrderManagerPlus.Logging;
using System.Windows.Controls;

namespace OrderManagerPlus.Windows
{
    public partial class EditOrderWindow : Window
    {
        private Order _order;
        private Order _originalOrder;
        private bool _isSaved = false;

        public event Action OrderUpdated;

        public EditOrderWindow(Order order)
        {
            InitializeComponent();
            _order = order;
            _originalOrder = new Order
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                TaskId = order.TaskId,
                OrderDate = order.OrderDate,
                DueDate = order.DueDate,
                Price = order.Price,
                Status = order.Status,
                Discount = order.Discount,
                DiscountType = order.DiscountType,
                Customer = order.Customer,
                Task = order.Task
            };
            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            PriceTextBox.Text = _order.Price.ToString();
            StatusComboBox.Text = _order.Status;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (decimal.TryParse(PriceTextBox.Text, out decimal price))
                {
                    if (price < 0)
                    {
                        MessageBox.Show("Ціна не може бути меншою за 0.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _order.Price = price;
                    _order.Status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString();

                    SQLiteDataAccess.UpdateOrder(_order);
                    Logger.Log($"Замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\" було оновлено. Нова ціна: {_order.Price}, новий статус: {_order.Status}");
                    OrderUpdated?.Invoke();
                    _isSaved = true;
                    MessageBox.Show("Замовлення успішно оновлено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Невірний формат ціни. Будь ласка, введіть коректне значення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (FormatException ex)
            {
                Logger.Log($"Помилка формату при збереженні замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\": {ex.Message}");
                MessageBox.Show("Невірний формат ціни. Будь ласка, введіть коректне значення.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при збереженні замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\": {ex.Message}");
                MessageBox.Show($"Сталася помилка при збереженні замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ManualPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString();
                if (status == "Виконано/не оплачено" || _order.Status == "Виконано/не оплачено")
                {
                    if (_order.Customer.Balance >= _order.Price && status != "Частково виконано")
                    {
                        ProcessManualPayment();
                    }
                    else if (_order.Customer.Balance < _order.Price)
                    {
                        MessageBox.Show("У замовника недостатньо коштів для оплати замовлення.", "Недостатній баланс", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Logger.Log($"Не вдалося виконати ручну оплату для замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\". Недостатньо коштів.");
                    }
                    else
                    {
                        MessageBox.Show("Замовлення не можна оплатити вручну, якщо воно не повністю виконано.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Logger.Log($"Не вдалося виконати ручну оплату для замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\". Замовлення не повністю виконано.");
                    }
                }
                else
                {
                    MessageBox.Show("Замовлення не можна оплатити вручну, якщо воно не має статусу 'Виконано/не оплачено'.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.Log($"Не вдалося виконати ручну оплату для замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\". Неправильний статус замовлення.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при ручній оплаті замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\": {ex.Message}");
                MessageBox.Show($"Сталася помилка при ручній оплаті замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProcessManualPayment()
        {
            try
            {
                _order.Customer.Balance -= _order.Price;
                _order.Status = "Виконано і оплачено";
                SQLiteDataAccess.UpdateOrder(_order);
                SQLiteDataAccess.UpdateCustomer(_order.Customer);
                Logger.Log($"Ручна оплата зроблена для замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\". Баланс замовника: {_order.Customer.Balance}");
                OrderUpdated?.Invoke();
                MessageBox.Show("Замовлення успішно оплачено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                _isSaved = true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при обробці ручної оплати для замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\": {ex.Message}");
                MessageBox.Show($"Сталася помилка при обробці ручної оплати: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Ви впевнені, що хочете видалити це замовлення?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    SQLiteDataAccess.DeleteOrder(_order.Id);
                    Logger.Log($"Замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\" було видалено.");
                    OrderUpdated?.Invoke();
                    MessageBox.Show("Замовлення успішно видалено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Помилка при видаленні замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\": {ex.Message}");
                MessageBox.Show($"Сталася помилка при видаленні замовлення: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!_isSaved)
            {
                // Відновити оригінальний стан замовлення при закритті без збереження
                _order.Price = _originalOrder.Price;
                _order.Status = _originalOrder.Status;
                Logger.Log($"Замовлення \"{_order.Task.Name}\" сфери \"{_order.Task.Category}\" групи \"{_order.Customer.Group}\" замовника \"{_order.Customer.FullName}\" закрито без збереження змін.");
            }
        }
    }
}