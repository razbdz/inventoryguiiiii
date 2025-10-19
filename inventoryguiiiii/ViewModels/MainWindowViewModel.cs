using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartSupplyGUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private double _totalRevenue;
        private readonly Inventory _inventory;
        private readonly Queue<Order> _orderQueue;

        public ObservableCollection<Order> ProcessedOrders { get; set; }
        public ObservableCollection<Order> QueuedOrders { get; set; }
        public ObservableCollection<Item> InventoryItems { get; set; }
        public ObservableCollection<Item> LowStockItems { get; set; }

        public ICommand ProcessNextOrderCommand { get; }

        public double TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            _inventory = new Inventory();
            _orderQueue = new Queue<Order>();
            ProcessedOrders = new ObservableCollection<Order>();
            QueuedOrders = new ObservableCollection<Order>();
            InventoryItems = new ObservableCollection<Item>();
            LowStockItems = new ObservableCollection<Item>();

            // Tilføj varer
            var rice = new Item("Rice", 20, 12);
            var sugar = new Item("Sugar", 15, 10);
            var chair = new Item("Chair", 50, 6);
            var bulb = new Item("Light Bulb", 33, 2);

            _inventory.AddStock(rice);
            _inventory.AddStock(sugar);
            _inventory.AddStock(chair);
            _inventory.AddStock(bulb);

            UpdateInventoryDisplay();

            // Opret ordrer
            var order1 = new Order("John");
            order1.AddLine(rice, 5);
            order1.AddLine(sugar, 3);

            var order2 = new Order("Emma");
            order2.AddLine(chair, 1);
            order2.AddLine(bulb, 1);

            _orderQueue.Enqueue(order1);
            _orderQueue.Enqueue(order2);

            QueuedOrders.Add(order1);
            QueuedOrders.Add(order2);

            ProcessNextOrderCommand = new RelayCommand(_ => ProcessNextOrder());
        }

        private void ProcessNextOrder()
        {
            if (_orderQueue.Count == 0)
                return;

            var order = _orderQueue.Dequeue();
            QueuedOrders.Remove(order);

            double orderTotal = 0;
            foreach (var line in order.Lines)
            {
                _inventory.RemoveStock(line.Item, line.Quantity);
                orderTotal += line.Item.Price * line.Quantity;
            }

            TotalRevenue += orderTotal;
            ProcessedOrders.Add(order);
            UpdateInventoryDisplay();
        }

        private void UpdateInventoryDisplay()
        {
            InventoryItems.Clear();
            foreach (var item in _inventory.GetAllItems())
                InventoryItems.Add(item);

            LowStockItems.Clear();
            foreach (var item in _inventory.GetLowStockItems(5))
                LowStockItems.Add(item);
        }

        // ---------- PropertyChanged support ----------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ---------- Hjælpeklasser ----------
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public RelayCommand(Action<object?> execute) => _execute = execute;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
    }

    public class Inventory
    {
        private readonly List<Item> _items = new();

        public void AddStock(Item item)
        {
            var existing = _items.FirstOrDefault(i => i.Name == item.Name);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                _items.Add(item);
        }

        public void RemoveStock(Item item, int quantity)
        {
            var existing = _items.FirstOrDefault(i => i.Name == item.Name);
            if (existing != null)
                existing.Quantity -= quantity;
        }

        public List<Item> GetAllItems() => _items;
        public List<Item> GetLowStockItems(int threshold) =>
            _items.Where(i => i.Quantity < threshold).ToList();
    }

    public class Order
    {
        public string Customer { get; }
        public List<OrderLine> Lines { get; }

        public Order(string customer)
        {
            Customer = customer;
            Lines = new List<OrderLine>();
        }

        public void AddLine(Item item, int quantity)
        {
            Lines.Add(new OrderLine(item, quantity));
        }

        public double Total => Lines.Sum(l => l.Item.Price * l.Quantity);
    }

    public class OrderLine
    {
        public Item Item { get; }
        public int Quantity { get; }

        public OrderLine(Item item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public class Item
    {
        public string Name { get; }
        public double Price { get; }
        public int Quantity { get; set; }

        public Item(string name, double price, int quantity)
        {
            Name = name;
            Price = price;
            Quantity = quantity;
        }
    }
}
