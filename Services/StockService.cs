using System.IO;
using MSM.Models;
using OfficeOpenXml;

namespace MSM.Services
{
    public class StockService : IStockService
    {
        private const string FilePath = "stock.xlsx";
        private const string WorksheetName = "Products";

        static StockService()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Personal Use");
            InitializeDatabase();
        }

        private static void InitializeDatabase()
        {
            if (!File.Exists(FilePath))
            {
                using (var package = new ExcelPackage(new FileInfo(FilePath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add(WorksheetName);
                    worksheet.Cells[1, 1].Value = "Barcode";
                    worksheet.Cells[1, 2].Value = "Name";
                    worksheet.Cells[1, 3].Value = "Quantity";
                    package.Save();
                }
            }
        }

        public Product GetProductByBarcode(string barcode)
        {
            using (var package = new ExcelPackage(new FileInfo(FilePath)))
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
                            Quantity = int.TryParse(worksheet.Cells[row, 3].Text, out var quantity) ? quantity : 0
                        };
                    }
                }
            }
            return null;
        }

        public void UpdateStock(string barcode, int quantity)
        {            
            using (var package = new ExcelPackage(new FileInfo(FilePath)))
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
            using (var package = new ExcelPackage(new FileInfo(FilePath)))
            {
                var worksheet = package.Workbook.Worksheets[WorksheetName];
                var row = worksheet.Dimension.End.Row + 1;

                worksheet.Cells[row, 1].Value = product.Barcode;
                worksheet.Cells[row, 2].Value = product.Name;
                worksheet.Cells[row, 3].Value = product.Quantity;

                package.Save();
            }
        }
    }
}
