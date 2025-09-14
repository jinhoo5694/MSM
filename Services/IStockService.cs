using MSM.Models;

namespace MSM.Services
{
    public interface IStockService
    {
        Product GetProductByBarcode(string barcode);
        void UpdateStock(string barcode, int quantity);
        void AddProduct(Product product);
    }
}
