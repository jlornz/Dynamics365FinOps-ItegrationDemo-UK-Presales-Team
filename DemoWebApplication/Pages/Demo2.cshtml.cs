using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Dynamics.DataEntities;
using Microsoft.OData.Client;
using Utilities;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace DemoWebApplication.Pages
{
    public class Demo2Model : PageModel
    {
        public string Message { get; set; }

        public string ProductID { get; set; }

        public List<SelectListItem> DistinctProducts { get; set; }
        public string OnhandData { get; private set; }
        public string NotAvailableMessage { get; private set; }

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

        public async Task<IEnumerable<SelectListItem>> GetDistinctProductsAsync()
        {
             Resources context = Authenticate();           

            var distinctProducts = await context.DistinctProducts.ExecuteAsync();

            
            DistinctProducts = new List<SelectListItem>(distinctProducts.Select(r => new SelectListItem
            {
                Value = r.ProductNumber.ToString(),
                Text = r.ProductName

            }));

            return DistinctProducts;
        }        

        public async Task<IActionResult> OnPostAsyncOdata(string productID)
        {
            Resources context = Authenticate();

            var productsOnhad = await context.InventorySitesOnHand.ExecuteAsync();

            var productOnhad = productsOnhad.Where(x => x.ItemNumber == productID).FirstOrDefault();

            GetOnhadArray(productOnhad);

            return null;
        }
        //Demo 2.1
        public async Task<IActionResult> OnPostAsync(string productID)
        {
            try
            {
                string clientId = ConnectionSettings.Default.ApimClientId;
                string ClientSecreet = ConnectionSettings.Default.ApinClientSecreet;
                string tenant = ConnectionSettings.Default.ApinOauthTenant;
                string resource = ConnectionSettings.Default.ActiveDirectoryResource;
                string subscriptionKey = ConnectionSettings.Default.ApinSubscriptionKey;

                //1. Authenticate via custom OAuth 2.0 service
                AuthenticationContext authenticationContext = new AuthenticationContext(tenant, false);

                var creadential = new ClientCredential(clientId, ClientSecreet);
                AuthenticationResult authenticationResult = authenticationContext.AcquireTokenAsync(resource, creadential).Result;

                        
                var authorizationToken = authenticationResult.AccessTokenType + " "  + authenticationResult.AccessToken;

                //2. Call Azure API        
                var client = new HttpClient();

                // Create headers
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                client.DefaultRequestHeaders.Add("Authorization", authorizationToken);

                string uri = "https://dyn365.azure-api.net/Warehouses/OnHand/" + productID;

                //Request call
                var request = await client.GetAsync(uri);
                string responseBody = await request.Content.ReadAsStringAsync();

                SetOnHandArray(responseBody);
            }
            catch ( Exception e)
            {
                throw new Exception(e.Message);
            }
            return null;
        }

        private void SetOnHandArray(string responseBody)
        {
            var o = JObject.Parse(responseBody);

            var values = o["value"];

            if (values.Count() == 0)
            {
                OnhandData = null;
                NotAvailableMessage = "Unavailable ☹";
            }
            else
            {
                values = values.First();

                if (values != null && ((decimal)values["OnHandQuantity"] + (decimal)values["ReservedOnHandQuantity"] + (decimal)values["OrderedQuantity"]) > 0)
                {
                    OnhandData = "[ " + (decimal)values["OnHandQuantity"] + " , " + (decimal)values["ReservedOnHandQuantity"] + " , " + (decimal)values["OrderedQuantity"] + " ]";
                    NotAvailableMessage = null;
                }
                else
                {
                    OnhandData = null;
                    NotAvailableMessage = "Unavailable ☹";
                }
            }
        }

        private void GetOnhadArray(InventorySiteOnHand productOnhad)
        {
            if (productOnhad != null && (productOnhad.OnHandQuantity + productOnhad.ReservedOnHandQuantity + productOnhad.OrderedQuantity) > 0)
            {
                OnhandData = "[ " + productOnhad.OnHandQuantity + " , " + productOnhad.ReservedOnHandQuantity + " , " + productOnhad.OrderedQuantity + " ]";
                NotAvailableMessage = null;
            }
            else
            {
                OnhandData = null;
                NotAvailableMessage = "Unavailable ☹";
            }
        }
    }
}
