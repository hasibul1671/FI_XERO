// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.XeroJob
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model.Dynamics;
using FinanceIntegration.Model.Xero;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Web.Http.Results;
using Xero.Api.Core;
using Xero.Api.Core.Model;
using Xero.Api.Example.Applications.Private;
using Xero.Api.Infrastructure.Exceptions;
using Xero.Api.Infrastructure.Interfaces;
using Xero.Api.Infrastructure.Model;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Serialization;
using Xero.Api.Core.Model.Types;
using XeroApiMidpoint;
using System.IO;

namespace FinanceIntegration.Model
{
    public class XeroJob
    {
        public static void Process(FinanceIntegration.Model.Dynamics.PurchaseOrder purchaseOrder, TraceWriter log)
        {
            log.Info("Starting Xero Process");
            FinanceIntegration.Model.Dynamics.Account supplier = new FinanceIntegration.Model.Dynamics.Account(purchaseOrder.Supplier.Id);
            log.Info("Fetched " + supplier.Name);
            while (supplier.ParentAccount != null)
            {
                log.Info("Found a parent");
                supplier = new FinanceIntegration.Model.Dynamics.Account(supplier.ParentAccount.Id);
                log.Info("Fetched " + supplier.Name);
            }
            log.Info("Bill to be for " + supplier.Name);
            EntityCollection disbursements = Disbursement.getDisbursements(purchaseOrder.Id);
            XeroTrackingCode xeroTrackingCode = new XeroTrackingCode(purchaseOrder.XeroTrackingCode.Id);
            Contact xeroContact = XeroContact.buildContact(supplier, log);
            List<LineItem> lineItems = XeroItem.BuildItems(disbursements, xeroTrackingCode, log);
            Invoice invoice = XeroInvoice.BuildInvoice(purchaseOrder, xeroContact, lineItems, disbursements, log);

            string jsonPayload = JsonConvert.SerializeObject(invoice);
            var bill = JObject.Parse(jsonPayload);
            bill.Property("Type").Remove();
            bill["Type"] = XeroInvoice.ParseInvoiceType(invoice.Type); // "ACCPAY"; 
            bill.Property("Status").Remove();
            bill["Status"] = XeroInvoice.ParseInvoiceStatus(invoice.Status);
            bill.Property("LineAmountTypes").Remove();
            bill["LineAmountTypes"] = XeroInvoice.ParseLineAmountType(invoice.LineAmountTypes); // "Exclusive";
            jsonPayload = bill.ToString();

            try
            {
                //log.Info("Try to create Xero Invoice");
                //X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                //x509Store.Open(OpenFlags.ReadOnly);
                //X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, (object)Configuration.CertKey, false);
                //X509Certificate2 certificate = (X509Certificate2)null;
                //if (certificate2Collection.Count > 0)
                //    certificate = certificate2Collection[0];
                //else
                //    log.Info("No Cert Found - We got some issues.");
                //x509Store.Close();
                //XeroCoreApi xeroCoreApi = new XeroCoreApi("https://api.xero.com", (IAuthenticator)new XeroApiMidpoint.PrivateAuthenticator(certificate), (IConsumer)new Consumer(Configuration.ConsumerKey, Configuration.ConsumerSecret), (IUser)null, (IJsonObjectMapper)new DefaultMapper(), (IXmlObjectMapper)new DefaultMapper());
                
                string accessToken = "";                                                  
                string tenantId = null;

                string retrievedAccessToken = XeroClient.getAccessToken(); // Will generate a new token and save it to the file
                if (retrievedAccessToken == null)
                {
                   log.Info("The Access Token was not retrieved properly.");
                }
                else
                {
                    accessToken = retrievedAccessToken;
                }

                tenantId = XeroClient.getOrganization(accessToken);
                if (tenantId == null)
                {
                    log.Info("XeroClient : Error getting organization ID needed to post payloads as per Xero Api guide");

                }

                if (invoice.Id == Guid.Empty)
                {
                    Invoice createdInvoice = XeroClient.postInvoice(tenantId, accessToken, jsonPayload);
                    log.Info("Created Invoice");


                    if (supplier.XeroId == string.Empty)
                    {
                        log.Info("Created new Contact - Updating CRM");
                        XeroContact.updateAccount(supplier.Id, createdInvoice.Contact.Id, log);
                    }
                    XeroJob.updateCreatedPurchaseOrder(purchaseOrder, createdInvoice, log);

                    //Invoice createdInvoice = xeroCoreApi.Invoices.Create(invoice);
                    //log.Info("Created Invoice");
                    //if (supplier.XeroId == string.Empty)
                    //{
                    //    log.Info("Created new Contact - Updating CRM");
                    //    XeroContact.updateAccount(supplier.Id, createdInvoice.Contact.Id, log);
                    //}
                    //XeroJob.updateCreatedPurchaseOrder(purchaseOrder, createdInvoice, log);
                }
                else
                {
                    Invoice returnInvoice = XeroClient.postInvoice(tenantId,accessToken,jsonPayload);
                    XeroJob.updatePurchaseOrder(purchaseOrder, returnInvoice, log);

                    //Invoice returnInvoice = xeroCoreApi.Invoices.Update(invoice);
                    //XeroJob.updatePurchaseOrder(purchaseOrder, returnInvoice, log);
                    //log.Info("Updated Invoice");
                }
            }
            catch (ValidationException ex)
            {
                StringBuilder stringBuilder = new StringBuilder("<p>ERRORS START BELOW</p>");
                foreach (ValidationError validationError in ex.ValidationErrors)
                    stringBuilder.Append("<p>" + validationError.Message + "</p>");
                Email.SendEmail(purchaseOrder.InitiatingUserId, purchaseOrder.InitiatingUserId, purchaseOrder.FundApplication, purchaseOrder.Name + " - Push to Xero - Error", stringBuilder.ToString());
                new WebProxyClient().Update(new Entity("illumina_purchaseorder", purchaseOrder.Id)
                {
                    ["illumina_purchaseorderstatus"] = (object)new OptionSetValue(390950007)
                });
            }
            catch (RateExceededException ex)
            {
                log.Info("Xero has hit the limit for - " + ex.RateLimitProblem);
                log.Info("Going to sleep for 90 seconds");
                Thread.Sleep(90000);
                log.Info("Finished sleeping for 90 seconds - Now will throw exception");
                throw;
            }
        }

        public static void updateCreatedPurchaseOrder(
          FinanceIntegration.Model.Dynamics.PurchaseOrder purchaseOrder,
          Invoice createdInvoice,
          TraceWriter log)
        {
            log.Info("Entered - updateCreatedPurchaseOrder - Filling in CRM with created info");
            string str = "https://go.xero.com/organisationlogin/default.aspx?shortcode=" + Configuration.XeroShortCode + "&redirecturl=/AccountsPayable/Edit.aspx?InvoiceID=" + createdInvoice.Id.ToString();
            new WebProxyClient().Update(new Entity("illumina_purchaseorder", purchaseOrder.Id)
            {
                ["illumina_xeronumber"] = (object)createdInvoice.Number,
                ["illumina_totalxerocredited"] = (object)new Money(createdInvoice.AmountCredited.GetValueOrDefault()),
                ["illumina_totalxeropaid"] = (object)new Money(createdInvoice.AmountPaid.GetValueOrDefault()),
                ["illumina_outstandingbalance"] = (object)new Money(createdInvoice.AmountDue.GetValueOrDefault()),
                ["illumina_xeroid"] = (object)createdInvoice.Id.ToString(),
                ["illumina_purchaseorderstatus"] = (object)new OptionSetValue(390950003),
                ["illumina_xerolink"] = (object)str,
                ["illumina_xerostatus"] = (object)new OptionSetValue((int)createdInvoice.Status)
            });
            log.Info("Exiting - updateCreatedPurchaseOrder");
        }

        public static void updatePurchaseOrder(
          FinanceIntegration.Model.Dynamics.PurchaseOrder purchaseOrder,
          Invoice returnInvoice,
          TraceWriter log)
        {
            log.Info("Entered - updatePurchaseOrder - Filling in CRM with created info");
            string str = "https://go.xero.com/organisationlogin/default.aspx?shortcode=" + Configuration.XeroShortCode + "&redirecturl=/AccountsPayable/Edit.aspx?InvoiceID=" + returnInvoice.Id.ToString();
            new WebProxyClient().Update(new Entity("illumina_purchaseorder", purchaseOrder.Id)
            {
                ["illumina_totalxerocredited"] = (object)new Money(returnInvoice.AmountCredited.GetValueOrDefault()),
                ["illumina_totalxeropaid"] = (object)new Money(returnInvoice.AmountPaid.GetValueOrDefault()),
                ["illumina_outstandingbalance"] = (object)new Money(returnInvoice.AmountDue.GetValueOrDefault()),
                ["illumina_purchaseorderstatus"] = (object)new OptionSetValue(390950003),
                ["illumina_xerolink"] = (object)str
            });
            log.Info("Exiting - updatePurchaseOrder");
        }        
    }
}
