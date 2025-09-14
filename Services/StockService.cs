using System;
using System.Collections.Generic;
using System.IO;
using MSM.Models;
using OfficeOpenXml;

namespace MSM.Services
{
    public class StockService : IStockService
    {
        private readonly string _filePath;
        private const string WorksheetName = "Products";

        public StockService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Personal Use");
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "stock.xlsx");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileInfo = new FileInfo(_filePath);
            if (!fileInfo.Exists)
            {
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.Add(WorksheetName);
                    worksheet.Cells[1, 1].Value = "Barcode";
                    worksheet.Cells[1, 2].Value = "Name";
                    worksheet.Cells[1, 3].Value = "Quantity";
                    worksheet.Cells[1, 4].Value = "ImagePath";
                    worksheet.Cells[1, 5].Value = "DefaultReductionAmount";
                    
                    // Add sample data
                    worksheet.Cells[2, 1].Value = "12345";
                    worksheet.Cells[2, 2].Value = "Product A";
                    worksheet.Cells[2, 3].Value = 10;
                    worksheet.Cells[2, 4].Value = "avares://MSM/Assets/product_a.png"; // Example image path
                    worksheet.Cells[2, 5].Value = 1;
                    
                    worksheet.Cells[3, 1].Value = "67890";
                    worksheet.Cells[3, 2].Value = "Product B";
                    worksheet.Cells[3, 3].Value = 20;
                    worksheet.Cells[3, 4].Value = "avares://MSM/Assets/product_b.png"; // Example image path
                    worksheet.Cells[3, 5].Value = 5;
                    
                    package.Save();
                }
            }
        }

        public Product GetProductByBarcode(string barcode)
        {
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                if (worksheet == null) return null;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text.Equals(barcode, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return new Product
                        {
                            Barcode = worksheet.Cells[row, 1].Text,
                            Name = worksheet.Cells[row, 2].Text,
                            Quantity = int.TryParse(worksheet.Cells[row, 3].Text, out var quantity) ? quantity : 0,
                            ImagePath = worksheet.Cells[row, 4].Text,
                            DefaultReductionAmount = int.TryParse(worksheet.Cells[row, 5].Text, out var defaultReductionAmount) ? defaultReductionAmount : 1
                        };
                    }
                }
            }
            return null;
        }

        public void UpdateStock(string barcode, int quantity)
        {            
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                if (worksheet == null) return;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text.Equals(barcode, System.StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.Cells[row, 3].Value = quantity;
                        package.Save();
                        return;
                    }
                }
            }
        }

        public void AddProduct(Product product)
        {
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                var row = worksheet.Dimension.End.Row + 1;

                worksheet.Cells[row, 1].Value = product.Barcode;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Quantity;
                worksheet.Cells[row, 4].Value = product.ImagePath;
                worksheet.Cells[row, 5].Value = product.DefaultReductionAmount;

                package.Save();
            }
        }

        public void UpdateProduct(Product product)
        {
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                if (worksheet == null) return;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text.Equals(product.Barcode, System.StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.Cells[row, 2].Value = product.Name;
                        worksheet.Cells[row, 3].Value = product.Quantity; // Quantity might not be updated from edit screen, but keep for consistency
                        worksheet.Cells[row, 4].Value = product.ImagePath;
                        worksheet.Cells[row, 5].Value = product.DefaultReductionAmount;
                        package.Save();
                        return;
                    }
                }
            }
        }

        public void SaveProducts(IEnumerable<Product> products)
        {
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                if (worksheet == null)
                {
                    worksheet = package.Workbook.Worksheets.Add(WorksheetName);
                    worksheet.Cells[1, 1].Value = "Barcode";
                    worksheet.Cells[1, 2].Value = "Name";
                    worksheet.Cells[1, 3].Value = "Quantity";
                    worksheet.Cells[1, 4].Value = "ImagePath";
                    worksheet.Cells[1, 5].Value = "DefaultReductionAmount";
                }
                else
                {
                    // Clear existing data, but keep header
                    worksheet.Cells[2, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column].Clear();
                }

                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value = product.Barcode;
                    worksheet.Cells[row, 2].Value = product.Name;
                    worksheet.Cells[row, 3].Value = product.Quantity;
                    worksheet.Cells[row, 4].Value = product.ImagePath;
                    worksheet.Cells[row, 5].Value = product.DefaultReductionAmount;
                    row++;
                }
                package.Save();
            }
        }

        public IEnumerable<Product> GetAllProducts()
        {
            var products = new List<Product>();
            using (var package = new ExcelPackage(new FileInfo(_filePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                if (worksheet == null) return products;

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    products.Add(new Product
                    {
                        Barcode = worksheet.Cells[row, 1].Text,
                        Name = worksheet.Cells[row, 2].Text,
                        Quantity = int.TryParse(worksheet.Cells[row, 3].Text, out var quantity) ? quantity : 0,
                        ImagePath = worksheet.Cells[row, 4].Text,
                        DefaultReductionAmount = int.TryParse(worksheet.Cells[row, 5].Text, out var defaultReductionAmount) ? defaultReductionAmount : 1
                    });
                }
            }
            return products;
        }
    }
}
