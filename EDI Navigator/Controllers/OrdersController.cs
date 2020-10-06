using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Security.Policy;
using System.Xml.Linq;
using System.Xml.Schema;
using Newtonsoft.Json;
using EDI_Navigator.Models;

namespace EDI_Navigator.Controllers
{
    public class OrdersController : Controller
    {
        private List<string> messages = new List<string>();
        private string currentFileName = "";
        private bool flag = false;
        private List<string> Paths;

        public OrdersController()
        {
            Paths = new List<string>();
        }

        // GET: Orders
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Upload()
        {
            var files = Request.Files;

            for (int i = 0; i < files.Count; i++)
            {
                HttpPostedFileBase file = files[i];
                currentFileName = Path.GetFileName(file.FileName);

                string xmlPath = Server.MapPath($"~/App_Data/{currentFileName}");
                string xsdPath = Server.MapPath($"~/Models/Orders_850_875_940.xsd");

                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas.Add("http://www.spscommerce.com/RSX", xsdPath);
                settings.ValidationEventHandler += new ValidationEventHandler(OnValidationError);

                XmlReader reader = XmlReader.Create(file.InputStream, settings);
                while (reader.Read())
                { }
                reader.Close();

                if (flag)
                    messages.Add($"Schema validation failed for {currentFileName}");
                else
                {
                    if (!Paths.Contains(xmlPath))
                    {
                        if (System.IO.File.Exists(xmlPath))
                        {
                            System.IO.File.Delete(xmlPath);
                        }
                        file.SaveAs(xmlPath);
                        Paths.Add(xmlPath);
                        messages.Add($"Validation success for { currentFileName }");
                    }
                    messages.Add($"{ currentFileName } has been imported");
                }

                flag = false;
            }

            if (Paths.Count == 0)
                return View();
            else
            {
                var orders = LoadXmlOrders(Paths);
                var tradingPartners = LoadTradingPartners(orders);

                return View(tradingPartners);
            }
        }

        private void OnValidationError(object sender, ValidationEventArgs e)
        {
            flag = true;
            messages.Add($"ERROR : {currentFileName} -- { e.Message} ");
        }

        private List<Orders> LoadXmlOrders(List<string> paths)
        {
            var orders = new List<Orders>();

            foreach (var path in paths)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(path);

                string xmlString = xmlDocument.OuterXml;
                using (StringReader read = new StringReader(xmlString))
                {
                    Type outType = typeof(Orders);
                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        orders.Add((Orders)serializer.Deserialize(reader));
                        reader.Close();
                    }
                    read.Close();
                }
            }
            return orders;
        }

        private List<TradingPartner> LoadTradingPartners(List<Orders> orders)
        {
            var tradingPartnerIds = orders
                .SelectMany(x => x.Order)
                .Select(y => y.Header.OrderHeader.TradingPartnerId)
                .Distinct();
            var allOrders = orders.SelectMany(x => x.Order);
            var tradingPartners = new List<TradingPartner>();

            foreach (var id in tradingPartnerIds)
            {
                var tpOrders = allOrders.Where(x => x.Header.OrderHeader.TradingPartnerId == id);
                var tradingPartner = new TradingPartner()
                {
                    TradingPartnerId = id,
                    ViewOrders = tpOrders.Select(XmlOrderToViewOrder).ToList()
                };
                tradingPartners.Add(tradingPartner);
            }
            foreach (var tp in tradingPartners)
            {
                tp.TotalAmount = tp.ViewOrders.SelectMany(x => x.LineItems).Sum(y => y.OrderQty * y.PurchasePrice);
                tp.TotalLineItems = tp.ViewOrders.SelectMany(x => x.LineItems).Count();
            }
            return tradingPartners;
        }

        private ViewOrder XmlOrderToViewOrder(OrderType xmlOrder)
        {
            var viewOrder = new ViewOrder()
            {
                ReferenceID = xmlOrder.Header.References.FirstOrDefault().ReferenceID,
                PurchaseOrderNumber = xmlOrder.Header.OrderHeader.PurchaseOrderNumber,
                PurchaseOrderDate = xmlOrder.Header.OrderHeader.PurchaseOrderDateSpecified 
                    ? xmlOrder.Header.OrderHeader.PurchaseOrderDate.ToShortDateString()
                    : string.Empty,
                TradingPartnerId = xmlOrder.Header.OrderHeader.TradingPartnerId,
                LineItems = new List<LineItem>()
            };
            
            
            foreach (var lineItem in xmlOrder.LineItem)
            {
                viewOrder.LineItems.Add(
                    new LineItem()
                    {
                        SequenceNumber = lineItem.OrderLine.LineSequenceNumber,
                        BuyerPartNumber = lineItem.OrderLine.BuyerPartNumber,
                        ConsumerPackageCode = lineItem.OrderLine.ConsumerPackageCode,
                        OrderQty = lineItem.OrderLine.OrderQty,
                        OrderQtyUOM = lineItem.OrderLine.OrderQtyUOM.ToString(),
                        PurchasePrice = lineItem.OrderLine.PurchasePrice,
                        VendorPartNumber = lineItem.OrderLine.VendorPartNumber,
                        ShipDate = lineItem.OrderLine.ShipDateSpecified 
                            ? lineItem.OrderLine.ShipDate.ToShortDateString() 
                            : string.Empty
                    });
            }

            return viewOrder;
        }
    }
}