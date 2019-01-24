using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Dynamics.DataEntities;
using Microsoft.OData.Client;
using Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace DemoWebApplication.Pages
{
    public class Demo1Model : PageModel
    {
        private DataServiceResponse responce;
        [BindProperty]
        public string FailMessage { get; set; }
        [BindProperty]
        public string BathStatusCode { get;  set; }
        [BindProperty]
        public string Op1Status { get; set; }
        [BindProperty]
        public string Op2Status { get; set; }        
        public string Message { get; set; }
        [BindProperty]
        public SalesOrderHeader SalesOrderHeader { get; set; }      
        public SalesOrderLine SalesOrderLine { get; set; } = new SalesOrderLine();
        [BindProperty]
        public decimal ImputOrderQuantity { get; set; } = 1;
        [BindProperty]
        public string ImputOrderNumber { get; set; }
        [BindProperty]
        public string DeepLink { get; set; } = "";
        [BindProperty]
        public bool IsFail { get; set; }
        [BindProperty]
        public bool? IsPosted { get; set; } = null;
        public static List<Vendor> Customers { get; private set; } = new List<Vendor>();
        public List<SelectListItem> CustomerSelectItems { get; private set; }
        [BindProperty]
        public string SelectedCustomerAccount { get; set; }
        public Vendor SelectedCustomer { get; private set; }
        public List<DistinctProduct> Products { get; private set; }
        public List<SelectListItem> DistinctProducts { get; private set; }
        public string SelectedProductNumber{ get; set; }       
        public string IputProductNumber { get; set; }
        public DistinctProduct SelectedProduct { get; private set; }
        public string ProductNumber2Submit { get; private set; }

        public void OnGet() { }

        private static Resources Authenticate()
        {
            string ODataEntityPath = ConnectionSettings.Default.UriString + "data";
            Uri oDataUri = new Uri(ODataEntityPath, UriKind.Absolute);
            var context = new Resources(oDataUri);

            context.SendingRequest2 += new EventHandler<SendingRequest2EventArgs>(delegate (object sender, SendingRequest2EventArgs e)
            {
                var authenticationHeader = OAuthHandler.GetAuthenticationHeader(useWebAppAuthentication: true);
                e.RequestMessage.SetHeader(OAuthHandler.OAuthHeader, authenticationHeader);
            });
            return context;
        }

        //Demo 1.1
        public async Task<IEnumerable<SelectListItem>> GeCustomerAccountsAsync()
        {         
            Resources context = Authenticate();

            var customersList = await context.Vendors.ExecuteAsync();

            Customers = customersList.ToList();

            CustomerSelectItems = new List<SelectListItem>(Customers.Select(r => new SelectListItem
            {
                Value = r.VendorAccountNumber.ToString(),
                Text = r.VendorName + " - " + r.VendorAccountNumber,
            }));

            return CustomerSelectItems;
        }


        //Demo 1.2
        public async Task<IActionResult> OnPostSalesOrderLineAsync(string selectedCustomerAccount, string selectedProductNumber, string iputProductNumber)
        {
            Resources context = Authenticate();

            try
            {
                InitPurshaseOrderValues(selectedCustomerAccount, iputProductNumber, context);

                // 1. Single Changeset
                responce = await context.SaveChangesAsync(SaveChangesOptions.PostOnlySetProperties | SaveChangesOptions.BatchWithSingleChangeset);

                // 2. Independent Operations
                //responce = await context.SaveChangesAsync(SaveChangesOptions.PostOnlySetProperties | SaveChangesOptions.BatchWithIndependentOperations);

                BathStatusCode = responce.BatchStatusCode.ToString();
                Op1Status = responce.ElementAt(0).StatusCode.ToString();
                Op2Status = responce.ElementAt(1).StatusCode.ToString();
                DeepLink = GetDeepLinkString();

                IsPosted = true;
            }


            catch (DataServiceRequestException e)
            {
                IsPosted = false;

                var batchStatusCode = e.Response.BatchStatusCode;

                dynamic results = JsonConvert.DeserializeObject<dynamic>(e.InnerException.Message);
                var error = results.error.message;
                var errorDetails = results.error.innererror.message;
                FailMessage = error + " " + errorDetails;

                BathStatusCode = batchStatusCode.ToString();
                Op1Status = e.Response.ElementAt(0).StatusCode.ToString();
                Op2Status = Op1Status;
            }

            return null;
        }


        public async Task<IEnumerable<SelectListItem>> GetDistinctProductsAsync()
        {
            Resources context = Authenticate();

            var products = await context.DistinctProducts.ExecuteAsync();

            Products = products.ToList();

            DistinctProducts = new List<SelectListItem>(Products.Select(r => new SelectListItem
            {
                Value = r.ProductNumber.ToString(),
                Text = r.ProductName

            }));

            return DistinctProducts;
        }

        public void OnPostSalesOrderBody(string selectedCustomerAccount, string selectedProductNumber)
        {
            IputProductNumber = selectedProductNumber;
        }



        private string GetDeepLinkString()
        {
            Resources context = Authenticate();
            string sessionUrl = "/api/services/DeepLinkServices/DeepLinkSevice/getDeepLink";
            string GetUserSessionOperationPath = string.Format("{0}{1}", ConnectionSettings.Default.UriString.TrimEnd('/'), sessionUrl);

            var request = WebRequest.Create(GetUserSessionOperationPath);
            request.Headers[OAuthHandler.OAuthHeader] = OAuthHandler.GetAuthenticationHeader();
            request.Method = "POST";

            var requestContract = new
            {
                company = "usmf",
                menuItem = "PurchTableListPage",
                dataSource = "PurchTable",
                field = "PurchId",
                value = ImputOrderNumber
            };

            var requestContractString = JsonConvert.SerializeObject(requestContract);

            using (var stream = request.GetRequestStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(requestContractString);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream))
                    {
                        return streamReader.ReadToEnd().ToString();                       

                    }
                }
            }  
        }

        private void InitPurshaseOrderValues(string selectedCustomerAccount, string iputProductNumber, Resources context)
        {
            //Purchase Order Header
            PurchaseOrderHeader purchaseOrder = new PurchaseOrderHeader();
            DataServiceCollection<PurchaseOrderHeader> purchaseOrderCollection = new DataServiceCollection<PurchaseOrderHeader>(context);
            purchaseOrderCollection.Add(purchaseOrder);

            purchaseOrder.CurrencyCode = "USD";
            purchaseOrder.PurchaseOrderNumber = ImputOrderNumber;
            purchaseOrder.InvoiceVendorAccountNumber = selectedCustomerAccount;
            purchaseOrder.OrderVendorAccountNumber = selectedCustomerAccount;
            purchaseOrder.LanguageId = "en-us";
            purchaseOrder.dataAreaId = "USMF";

            //Purchase Order Line
            PurchaseOrderLine purchaseOrderLine = new PurchaseOrderLine();
            DataServiceCollection<PurchaseOrderLine> purchaseOrderLineCollection = new DataServiceCollection<PurchaseOrderLine>(context);

            purchaseOrderLineCollection.Add(purchaseOrderLine);
            purchaseOrderLine.PurchaseOrderNumber = ImputOrderNumber;
            purchaseOrderLine.ItemNumber = iputProductNumber;
            purchaseOrderLine.OrderedPurchaseQuantity = ImputOrderQuantity;
            purchaseOrderLine.dataAreaId = "USMF";
        }
    }

    public class DeepLickContract
    {
        public string Company { get; set; }
        public string MenuItem { get; set; }
        public string DataSource { get; set; }
        public string Field { get; set; }
        public string Value { get; set; }
    }
}
