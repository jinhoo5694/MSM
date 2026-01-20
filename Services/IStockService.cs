using System;
using System.Collections.Generic;
using MSM.Models;

namespace MSM.Services
{
    public interface IStockService
    {
        Product GetProductByBarcode(string barcode);
        void UpdateStock(string barcode, int quantity);
        void AddProduct(Product product);
        void UpdateProduct(Product product);
        void SaveProducts(IEnumerable<Product> products);
        IEnumerable<Product> GetAllProducts();
        void RecordStockChange(string barcode, int oldQuantity, int newQuantity, string reason = "");
        void ExportStockReport(string filePath);
        void DeleteProduct(string barcode);
        IEnumerable<StockChangeLogEntry> GetLogsByDateRange(DateTime startDate, DateTime endDate);
    }

    public class StockChangeLogEntry
    {
        public DateTime Time { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int OldQty { get; set; }
        public int NewQty { get; set; }
        public int ChangedQty => OldQty - NewQty;
        public string Reason { get; set; } = string.Empty;
    }
}
