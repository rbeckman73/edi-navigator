using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EDI_Navigator.Models
{
    public class Uploads
    {
        public List<string> Files { get; set; }
    }

    public class TradingPartner
    {
        public string TradingPartnerId { get; set; }
        public List<ViewOrder> ViewOrders { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalLineItems { get; set; }
    }

    public class ViewOrder
    {
        public string ReferenceID { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string PurchaseOrderDate { get; set; }
        public string TradingPartnerId { get; set; }
        public List<LineItem> LineItems { get; set; }
    }

    public class LineItem
    {
        public string SequenceNumber { get; set; }
        public string BuyerPartNumber { get; set; }
        public string VendorPartNumber { get; set; }
        public string ConsumerPackageCode { get; set; }
        public decimal OrderQty { get; set; }
        public string OrderQtyUOM { get; set; }
        public decimal PurchasePrice { get; set; }
        public string ShipDate { get; set; }
    }
}