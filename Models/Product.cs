namespace MSM.Models
{
    public class Product
    {
        public string? Barcode { get; set; }
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public int DefaultReductionAmount { get; set; } = 1; // Default to 1
        public int AlertQuantity { get; set; } = 1;
        public int SafeQuantity { get; set; } = 0;
    }
}
