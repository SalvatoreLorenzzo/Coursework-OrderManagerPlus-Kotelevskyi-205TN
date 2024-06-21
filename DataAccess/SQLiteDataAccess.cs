using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OrderManagerPlus.Models;
using OrderManagerPlus.Logging;

namespace OrderManagerPlus.DataAccess
{
    public static class SQLiteDataAccess
    {
        public static List<Customer> GetCustomers()
        {
            using (var db = new Context())
            {
                return db.Customers.ToList();
            }
        }

        public static List<Task> GetTasks()
        {
            using (var db = new Context())
            {
                return db.Tasks.ToList();
            }
        }

        public static List<Order> GetOrders()
        {
            using (var db = new Context())
            {
                return db.Orders.Include(o => o.Customer).Include(o => o.Task).ToList();
            }
        }

        public static void AddCustomer(Customer customer)
        {
            using (var db = new Context())
            {
                db.Customers.Add(customer);
                db.SaveChanges();
            }
        }

        public static void UpdateCustomer(Customer customer)
        {
            using (var db = new Context())
            {
                db.Customers.Update(customer);
                db.SaveChanges();
            }
        }

        public static void DeleteCustomer(int customerId)
        {
            using (var db = new Context())
            {
                var customer = db.Customers.Find(customerId);
                if (customer != null)
                {
                    db.Customers.Remove(customer);
                    db.SaveChanges();
                }
            }
        }

        public static void AddTask(Task task)
        {
            using (var db = new Context())
            {
                db.Tasks.Add(task);
                db.SaveChanges();
            }
        }

        public static void UpdateTask(Task task)
        {
            using (var db = new Context())
            {
                db.Tasks.Update(task);
                db.SaveChanges();
            }
        }

        public static void DeleteTask(int taskId)
        {
            using (var db = new Context())
            {
                var task = db.Tasks.Find(taskId);
                if (task != null)
                {
                    db.Tasks.Remove(task);
                    db.SaveChanges();
                }
            }
        }

        public static void AddOrder(Order order)
        {
            using (var db = new Context())
            {
                try
                {
                    db.Orders.Add(order);
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    Logger.Log($"Error adding order: {ex.InnerException?.Message ?? ex.Message}");
                    throw;
                }
            }
        }

        public static void UpdateOrder(Order order)
        {
            using (var db = new Context())
            {
                try
                {
                    db.Orders.Update(order);
                    db.SaveChanges();
                }
                catch (DbUpdateException ex)
                {
                    Logger.Log($"Error updating order: {ex.InnerException?.Message ?? ex.Message}");
                    throw;
                }
            }
        }

        public static void DeleteOrder(int orderId)
        {
            using (var db = new Context())
            {
                var order = db.Orders.Find(orderId);
                if (order != null)
                {
                    db.Orders.Remove(order);
                    db.SaveChanges();
                }
            }
        }
    }
}