using System;
using System.Collections.Generic;

namespace OrderManagerPlus.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Group { get; set; }
        public string Notes { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal Balance { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}