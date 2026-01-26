using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MSM.Models;
using OfficeOpenXml;
using LicenseContext = System.ComponentModel.LicenseContext;

namespace MSM.Services
{
    public class StockService : IStockService
    {
        private readonly string _filePath;
        private const string WorksheetName = "Products";

        // 변경 로그를 메모리에 저장
        private readonly List<StockChangeLog> _changeLogs = new();


        private readonly string _logFilePath = Path.Combine(AppContext.BaseDirectory, "stock_logs.json");

        public class StockChangeLog
        {
            public DateTime Time { get; set; }
            public string Barcode { get; set; }
            public string Name { get; set; }
            public int OldQty { get; set; }
            public int NewQty { get; set; }
            public string Reason { get; set; }
        }
        
        public StockService(string filePath)
        {
            _filePath = filePath;

            if (!File.Exists(_filePath))
            {
                using var package = new ExcelPackage(new FileInfo(_filePath));
                var ws = package.Workbook.Worksheets.Add(WorksheetName);
                ws.Cells[1, 1].Value = "Barcode";
                ws.Cells[1, 2].Value = "Name";
                ws.Cells[1, 3].Value = "Quantity";
                ws.Cells[1, 4].Value = "DefaultReductionAmount";
                ws.Cells[1, 5].Value = "ImagePath";
                ws.Cells[1, 6].Value = "AlertQuantity";
                ws.Cells[1, 7].Value = "SafeQuantity";
                package.Save();
            }

            if (File.Exists(_logFilePath))
            {
                var lines = File.ReadAllLines(_logFilePath);
                foreach (var line in lines)
                {
                    try
                    {
                        var log = System.Text.Json.JsonSerializer.Deserialize<StockChangeLog>(line);
                        if (log != null) _changeLogs.Add(log);
                    }
                    catch
                    {
                        // 무시
                    }
                }
            }
        }

        public Product GetProductByBarcode(string barcode)
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null) return null;

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                if (ws.Cells[row, 1].Text.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    return new Product
                    {
                        Barcode = ws.Cells[row, 1].Text,
                        Name = ws.Cells[row, 2].Text,
                        Quantity = int.TryParse(ws.Cells[row, 3].Text, out var q) ? q : 0,
                        DefaultReductionAmount = int.TryParse(ws.Cells[row, 4].Text, out var d) ? d : 1,
                        ImagePath = ws.Cells[row, 5].Text,
                        AlertQuantity = int.TryParse(ws.Cells[row, 6].Text, out var q2) ? q2 : 1,
                        SafeQuantity = int.TryParse(ws.Cells[row, 7].Text, out var d2) ? d2 : 0,
                    };
                }
            }

            return null;
        }

        public IEnumerable<Product> GetAllProducts()
        {
            var products = new List<Product>();
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null) return products;

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                if (string.IsNullOrWhiteSpace(ws.Cells[row, 1].Text)) continue;

                products.Add(new Product
                {
                    Barcode = ws.Cells[row, 1].Text,
                    Name = ws.Cells[row, 2].Text,
                    Quantity = int.TryParse(ws.Cells[row, 3].Text, out var q) ? q : 0,
                    DefaultReductionAmount = int.TryParse(ws.Cells[row, 4].Text, out var d) ? d : 1,
                    ImagePath = ws.Cells[row, 5].Text,
                    AlertQuantity = int.TryParse(ws.Cells[row, 6].Text, out var q2) ? q2 : 1,
                    SafeQuantity = int.TryParse(ws.Cells[row, 7].Text, out var d2) ? d2 : 0,
                });
            }

            return products;
        }


        public void AddProduct(Product product)
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];

            int row = ws.Dimension?.End.Row + 1 ?? 2;
            ws.Cells[row, 1].Value = product.Barcode;
            ws.Cells[row, 2].Value = product.Name;
            ws.Cells[row, 3].Value = product.Quantity;
            ws.Cells[row, 4].Value = product.DefaultReductionAmount;
            ws.Cells[row, 5].Value = product.ImagePath;
            ws.Cells[row, 6].Value = product.AlertQuantity;
            ws.Cells[row, 7].Value = product.SafeQuantity;
            package.Save();

            RecordStockChange(product.Barcode, 0, product.Quantity, "상품 추가");
        }

        public void UpdateProduct(Product product)
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null) return;

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                if (ws.Cells[row, 1].Text.Equals(product.Barcode, StringComparison.OrdinalIgnoreCase))
                {
                    int oldQty = int.TryParse(ws.Cells[row, 3].Text, out var q) ? q : 0;

                    ws.Cells[row, 2].Value = product.Name;
                    ws.Cells[row, 3].Value = product.Quantity;
                    ws.Cells[row, 4].Value = product.DefaultReductionAmount;
                    ws.Cells[row, 5].Value = product.ImagePath;
                    ws.Cells[row, 6].Value = product.AlertQuantity;
                    ws.Cells[row, 7].Value = product.SafeQuantity;
                    package.Save();

                    RecordStockChange(product.Barcode, oldQty, product.Quantity, "상품 정보 변경");
                    return;
                }
            }
        }

        public void UpdateStock(string barcode, int quantity)
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null) return;

            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                if (ws.Cells[row, 1].Text.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    int oldQty = int.TryParse(ws.Cells[row, 3].Text, out var q) ? q : 0;

                    ws.Cells[row, 3].Value = quantity;
                    package.Save();

                    string name = ws.Cells[row, 2].Text;
                    RecordStockChange(barcode, oldQty, quantity, "재고 수량 변경");
                    return;
                }
            }
        }

        public void DeleteProduct(string barcode)
        {
            // 안전 검사
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null || ws.Dimension == null)
                return;

            // 찾기
            for (int row = 2; row <= ws.Dimension.End.Row; row++)
            {
                var cellBarcode = ws.Cells[row, 1].Text;
                if (string.IsNullOrWhiteSpace(cellBarcode))
                    continue;

                if (cellBarcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                {
                    // 삭제 전 정보 수집
                    int oldQty = int.TryParse(ws.Cells[row, 3].Text, out var q) ? q : 0;
                    string name = ws.Cells[row, 2].Text ?? string.Empty;

                    // 로그를 메모리/파일에 즉시 기록 (GetProductByBarcode 호출 불필요)
                    var log = new StockChangeLog
                    {
                        Time = DateTime.Now,
                        Barcode = barcode,
                        Name = name,
                        OldQty = oldQty,
                        NewQty = 0,
                        Reason = "상품 삭제"
                    };
                    _changeLogs.Add(log);
                    try
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(log);
                        File.AppendAllText(_logFilePath, json + Environment.NewLine);
                    }
                    catch
                    {
                        // 파일 쓰기 실패는 무시하거나 로깅(예: Console.Error)해도 됩니다.
                    }

                    // 실제 행 삭제 및 저장
                    ws.DeleteRow(row);
                    package.Save();
                    return;
                }
            }
        }

        public void SaveProducts(IEnumerable<Product> products)
        {
            using var package = new ExcelPackage(new FileInfo(_filePath));
            var ws = package.Workbook.Worksheets[WorksheetName];
            if (ws == null) return;

            ws.Cells.Clear();
            ws.Cells[1, 1].Value = "Barcode";
            ws.Cells[1, 2].Value = "Name";
            ws.Cells[1, 3].Value = "Quantity";
            ws.Cells[1, 4].Value = "DefaultReductionAmount";
            ws.Cells[1, 5].Value = "ImagePath";
            ws.Cells[1, 6].Value = "AlertQuantity";
            ws.Cells[1, 7].Value = "SafeQuantity";

            int row = 2;
            foreach (var p in products)
            {
                ws.Cells[row, 1].Value = p.Barcode;
                ws.Cells[row, 2].Value = p.Name;
                ws.Cells[row, 3].Value = p.Quantity;
                ws.Cells[row, 4].Value = p.DefaultReductionAmount;
                ws.Cells[row, 5].Value = p.ImagePath;
                ws.Cells[row, 6].Value = p.AlertQuantity;
                ws.Cells[row, 7].Value = p.SafeQuantity;
                row++;
            }

            package.Save();
        }

        // 로그 기록
        public void RecordStockChange(string barcode, int oldQuantity, int newQuantity, string reason = "")
        {
            var product = GetProductByBarcode(barcode);
            var log = new StockChangeLog
            {
                Time = DateTime.Now,
                Barcode = barcode,
                Name = product?.Name ?? "",
                OldQty = oldQuantity,
                NewQty = newQuantity,
                Reason = reason
            };
            _changeLogs.Add(log);
            var json = System.Text.Json.JsonSerializer.Serialize(log);
            File.AppendAllText(_logFilePath, json + Environment.NewLine);
        }

        // 날짜 범위로 로그 조회
        public IEnumerable<StockChangeLogEntry> GetLogsByDateRange(DateTime startDate, DateTime endDate)
        {
            var results = new List<StockChangeLogEntry>();

            if (!File.Exists(_logFilePath))
                return results;

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var lines = File.ReadAllLines(_logFilePath);
            foreach (var line in lines)
            {
                try
                {
                    var log = System.Text.Json.JsonSerializer.Deserialize<StockChangeLog>(line, jsonOptions);
                    if (log != null && log.Time.Date >= startDate.Date && log.Time.Date <= endDate.Date)
                    {
                        results.Add(new StockChangeLogEntry
                        {
                            Time = log.Time,
                            Barcode = log.Barcode,
                            Name = log.Name,
                            OldQty = log.OldQty,
                            NewQty = log.NewQty,
                            Reason = log.Reason
                        });
                    }
                }
                catch
                {
                    // 잘못된 라인은 무시
                }
            }

            return results.OrderByDescending(x => x.Time);
        }

        // 보고서 내보내기
        public void ExportStockReport(string filePath)
        {
            using var package = new ExcelPackage();

            // 시트1: 현재 재고 현황
            var ws1 = package.Workbook.Worksheets.Add("전체 재고 현황");
            ws1.Cells[1, 1].Value = "바코드 번호";
            ws1.Cells[1, 2].Value = "상품명";
            ws1.Cells[1, 3].Value = "남은 재고 수량";
            ws1.Cells[1, 4].Value = "기본 차감 수량";
            ws1.Cells[1, 5].Value = "경고 수량";
            ws1.Cells[1, 6].Value = "안전 수량";

            int r = 2;
            foreach (var p in GetAllProducts())
            {
                ws1.Cells[r, 1].Value = p.Barcode;
                ws1.Cells[r, 2].Value = p.Name;
                ws1.Cells[r, 3].Value = p.Quantity;
                ws1.Cells[r, 4].Value = p.DefaultReductionAmount;
                ws1.Cells[r, 5].Value = p.AlertQuantity;
                ws1.Cells[r, 6].Value = p.SafeQuantity;
                r++;
            }

            // 시트2: 변경 로그
            var ws2 = package.Workbook.Worksheets.Add("변경 이력");
            ws2.Cells[1, 1].Value = "변경 시간";
            ws2.Cells[1, 2].Value = "바코드 번호";
            ws2.Cells[1, 3].Value = "상품명";
            ws2.Cells[1, 4].Value = "변경 전 수량";
            ws2.Cells[1, 5].Value = "변경 후 수량";
            ws2.Cells[1, 6].Value = "변경 수량";
            ws2.Cells[1, 7].Value = "비고";

            if (File.Exists(_logFilePath))
            {
                var lines = File.ReadAllLines(_logFilePath);
                int r2 = 2;
                foreach (var line in lines)
                {
                    try
                    {
                        var log = System.Text.Json.JsonSerializer.Deserialize<StockChangeLog>(line);
                        if (log != null)
                        {
                            ws2.Cells[r2, 1].Value = log.Time.ToString("yyyy-MM-dd HH:mm:ss");
                            ws2.Cells[r2, 2].Value = log.Barcode;
                            ws2.Cells[r2, 3].Value = log.Name;
                            ws2.Cells[r2, 4].Value = log.OldQty;
                            ws2.Cells[r2, 5].Value = log.NewQty;
                            ws2.Cells[r2, 6].Value = log.NewQty - log.OldQty; // 변경 수량
                            ws2.Cells[r2, 7].Value = log.Reason;
                            r2++;
                        }
                    }
                    catch
                    {
                        // 잘못된 라인은 무시
                    }
                }
            }

            package.SaveAs(new FileInfo(filePath));
        }
    }
}
