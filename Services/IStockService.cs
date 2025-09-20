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
    }
}
