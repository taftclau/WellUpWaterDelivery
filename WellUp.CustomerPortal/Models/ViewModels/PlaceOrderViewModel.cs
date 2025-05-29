namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class PlaceOrderViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int AddressId { get; set; }
        public DateTime PreferredDeliveryTime { get; set; }
        public string Notes { get; set; }
    }
}