namespace Dashboards_reports.Models.MaterialPlanning
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public decimal Quantity { get; set; } // This is your On-Hand Qty
        public decimal? MinInventory { get; set; } = 0; // Optional safety stock per item/location
    }
}
