using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Xero.Api.Core.Model;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using Xero.Api.Infrastructure.ThirdParty.ServiceStack.Text;

namespace FinanceIntegration.Model.Xero
{
    internal class XeroClient
    {
        public XeroClient() { }
        
        public static string getAccessToken() {
            AzureTableConfiguration table = new AzureTableConfiguration();

            using (HttpClient client = new HttpClient())
            {
                string accessToken = "";
                string accessTokenUrl = "https://identity.xero.com/connect/token";
                string clientId = Configuration.XeroClientId;
                string clientSecret = Configuration.XeroClientSecret;                
                string grantTypeRefreshToken = "refresh_token";
                string refreshToken = table.First().RefreshToken; //Reads the table XeroAccess to get the last refreshToken saved // If this azure function not used in 60 days this will expire. This is valid for 60 days, for us to have it correct we need to use the illuminance reusables console app called xero api tokens. There will be a step by step guide to generate this token. 


                string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientId + ":" + clientSecret));
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);

                var parameters = new Dictionary<string, string>
                {
                    { "grant_type", grantTypeRefreshToken },
                    { "refresh_token", refreshToken }
                };

                var content = new FormUrlEncodedContent(parameters);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                var response = client.PostAsync(accessTokenUrl, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    var tokensResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result);

                    accessToken = tokensResponse.access_token;
                    refreshToken = tokensResponse.refresh_token;                                      

                    table.First().RefreshToken = refreshToken; // Insert the new refresh token to the table

                    table.Update(); //update the table with the new refresh token

                    return accessToken;
                }
                else 
                {                    
                    return null;
                }               
            }

        }


        public static string getOrganization(string accessToken) {

            using (HttpClient client = new HttpClient())
            {              
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var getOganizationsUrl = "https://api.xero.com/connections";

                var response = client.GetAsync(getOganizationsUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    var organizationIdResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result);
                    var tenantId = organizationIdResponse[0].tenantId;

                    return tenantId;
                }
                else
                {
                   return null;
                }
            }

        }


        public static XeroResponse getContacts(string accessToken, string tenantId)
        {
            dynamic payload = null;

            using (HttpClient client = new HttpClient())
            {
                var tenantIdHeaderAttribute = "Xero-tenant-id";
                client.DefaultRequestHeaders.Add(tenantIdHeaderAttribute, tenantId);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var getContacts = "https://api.xero.com/api.xro/2.0/Contacts";

                var response = client.GetAsync(getContacts).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    // Configure JsonSerializerSettings with MissingMemberHandling set to Ignore
                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    payload = Newtonsoft.Json.JsonConvert.DeserializeObject<XeroResponse>(result,settings);

                    return payload;
                }
                else
                {
                    return null;
                }
            }
        }

        public static XeroProject postProject(string tenantId, string accessToken, object projectJson)
        {

           string jsonPayload = projectJson.ToString();
            XeroProject createdProject = null;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Xero-tenant-id", tenantId);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var postProjectUrl = "https://api.xero.com/projects.xro/2.0/Projects";

                var response = client.PostAsync(postProjectUrl, new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    createdProject = JsonConvert.DeserializeObject<XeroProject>(result, settings);
                    return createdProject;
                }
                else
                {
                    var error = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine("Project creation failed: " + error);
                    return null;
                }
            }
        }





        public static Invoice postInvoice(string tenantId, string accessToken, string jsonData)
        {
            Invoice createdInvoice = null;
            //Http that creates the invoice in xero
            using (HttpClient client = new HttpClient())
            {
                //Must always include the tenant id in the header otherwise it won't work
                var tenantIdHeaderAttribute = "Xero-tenant-id";
                client.DefaultRequestHeaders.Add(tenantIdHeaderAttribute, tenantId);
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);

                var postInvoiceUrl = "https://api.xero.com/api.xro/2.0/Invoices";

                var response = client.PostAsync(postInvoiceUrl, new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json")).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    result = result.Replace("ACCPAY", "0");
                    result = result.Replace("ACCREC", "1");


                    result = result.Replace("POBOX", "0");
                    result = result.Replace("STREET", "1");

                    result = result.Replace("DEFAULT", "0");
                    result = result.Replace("DDI", "1");
                    result = result.Replace("MOBILE", "2");
                    result = result.Replace("FAX", "3");

                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    JObject payloadObject = JObject.Parse(result);

                    // Remove the "PaymentTerms" property
                    payloadObject["Invoices"]?[0]?["Contact"]?.Value<JObject>("PaymentTerms")?.Parent?.Remove();


                    // Convert the modified JObject back to a JSON string
                    string modifiedPayloadJson = payloadObject.ToString();

                    createdInvoice = JsonConvert.DeserializeObject<XeroResponse>(modifiedPayloadJson, settings).Invoices.FirstOrDefault();
                    return createdInvoice;
                }
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    return null;
                }
            }
        }



        public static Invoice getInvoices(string accessToken, string tenantId, string invoiceId, TraceWriter log)
        {
            

            using (HttpClient client = new HttpClient())
            {
                Invoice invoice = null;

                var tenantIdHeaderAttribute = "Xero-tenant-id";
                client.DefaultRequestHeaders.Add(tenantIdHeaderAttribute, tenantId);

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var getInvoices = "https://api.xero.com/api.xro/2.0/Invoices";
                var finalUrl = getInvoices + "/" + invoiceId;

                var response = client.GetAsync(finalUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    result = result.Replace("ACCPAY", "0");
                    result = result.Replace("ACCREC", "1");


                    result = result.Replace("POBOX", "0");
                    result = result.Replace("STREET", "1");

                    result = result.Replace("DEFAULT", "0");
                    result = result.Replace("DDI", "1");
                    result = result.Replace("MOBILE", "2");
                    result = result.Replace("FAX", "3");

                    var settings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };

                    //THIS TRY WAS TEST PURPOSES, NOT NEEDED IN THE CODE
                    //try
                    //{
                    //    log.Info("HTTP RESPONSE COMMING FROM XEROCLIENT: ");
                    //    log.Info("RESPONSE: " + result);
                    //}
                    //catch (Exception)
                    //{
                    //}


                    invoice = JsonConvert.DeserializeObject<XeroResponse>(result, settings).Invoices.FirstOrDefault();
                    return invoice;
                }
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    return null;
                }
            }

        }

      
    }
}
