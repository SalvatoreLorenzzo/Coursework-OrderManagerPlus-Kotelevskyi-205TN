using OrderManagerPlus.Models;
using System;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int TaskId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } // "Виконано і оплачено", "Виконано/не оплачено", "Частково виконано", "Не виконано"
    public decimal Discount { get; set; }
    public string DiscountType { get; set; } // "percentage" or "amount"

    public Customer Customer { get; set; }
    public Task Task { get; set; }
}