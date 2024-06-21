using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OrderManagerPlus
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand ToggleMenu = new RoutedUICommand(
            "ToggleMenu", "ToggleMenu", typeof(CustomCommands));

        public static readonly RoutedUICommand AddCustomer = new RoutedUICommand(
            "AddCustomer", "AddCustomer", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.C, ModifierKeys.Control) });

        public static readonly RoutedUICommand AddTask = new RoutedUICommand(
            "AddTask", "AddTask", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.T, ModifierKeys.Control) });

        public static readonly RoutedUICommand AddOrder = new RoutedUICommand(
            "AddOrder", "AddOrder", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.O, ModifierKeys.Control) });

        public static readonly RoutedUICommand Settings = new RoutedUICommand(
            "Settings", "Settings", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.I, ModifierKeys.Control) });

        public static readonly RoutedUICommand Statistics = new RoutedUICommand(
            "Statistics", "Statistics", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.S, ModifierKeys.Control) });

        public static readonly RoutedUICommand Payment = new RoutedUICommand(
            "Payment", "Payment", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.P, ModifierKeys.Control) });

        public static readonly RoutedUICommand SetStatusNotCompleted = new RoutedUICommand(
            "SetStatusNotCompleted", "SetStatusNotCompleted", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.D1, ModifierKeys.Control) });

        public static readonly RoutedUICommand SetStatusPartiallyCompleted = new RoutedUICommand(
            "SetStatusPartiallyCompleted", "SetStatusPartiallyCompleted", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.D2, ModifierKeys.Control) });

        public static readonly RoutedUICommand SetStatusNotPaid = new RoutedUICommand(
            "SetStatusNotPaid", "SetStatusNotPaid", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.D3, ModifierKeys.Control) });

        public static readonly RoutedUICommand SetStatusPaid = new RoutedUICommand(
            "SetStatusPaid", "SetStatusPaid", typeof(CustomCommands), new InputGestureCollection { new KeyGesture(Key.D4, ModifierKeys.Control) });
    }
}
