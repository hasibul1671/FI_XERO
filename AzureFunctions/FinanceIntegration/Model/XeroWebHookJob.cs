// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.XeroWebHookJob
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model.Xero;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Xero.Api.Core;
using Xero.Api.Core.Model;
using Xero.Api.Core.Model.Status;
using Xero.Api.Core.Model.Types;
using Xero.Api.Example.Applications.Private;
using Xero.Api.Infrastructure.Exceptions;
using Xero.Api.Infrastructure.Interfaces;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Serialization;


namespace FinanceIntegration.Model
{
    public static class XeroWebHookJob
    {
        public static void Process(XeroEvent newEvent, TraceWriter log)
        {
            //log.Info("Starting XeroWebHook Process");
            //X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            //x509Store.Open(OpenFlags.ReadOnly);
            //X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, (object)Configuration.CertKey, false);
            //X509Certificate2 certificate = (X509Certificate2)null;
            //if (certificate2Collection.Count > 0)
            //    certificate = certificate2Collection[0];
            //else
            //    log.Info("No Cert Found - We got some issues.");
            //x509Store.Close();
            //XeroCoreApi xeroCoreApi = new XeroCoreApi("https://api.xero.com", (IAuthenticator)new PrivateAuthenticator(certificate), (IConsumer)new Consumer(Configuration.ConsumerKey, Configuration.ConsumerSecret), (IUser)null, (IJsonObjectMapper)new DefaultMapper(), (IXmlObjectMapper)new DefaultMapper());
            //log.Info("Successfully Connected to Xero");

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

            try
            {
                //Invoice invoice = xeroCoreApi.Invoices.Find(newEvent.ResourceId);
                Invoice invoice = XeroClient.getInvoices(accessToken, tenantId, newEvent.ResourceId.ToString(),log);

                //Try catch Developed only for test, doesn't affect the code functioning, can be deleted
                try
                {
                    log.Info("Found Invoice - " + invoice.Id.ToString());

                    log.Info("Invoice class to JSON:");
                    log.Info(JsonConvert.SerializeObject(invoice));

                    log.Info("Invoice class to string:");
                    log.Info(invoice.ToString());
                 
                }
                catch (Exception)
                {

           
                }           


                if (invoice.Type == InvoiceType.AccountsPayable)
                {
                    log.Info("Invoice is ACCPAY- LETS GO!");
                    QueryExpression query = new QueryExpression("illumina_purchaseorder");
                    query.ColumnSet.AddColumns("illumina_name", "illumina_totalxerocredited", "illumina_totalxeropaid", "illumina_outstandingbalance", "illumina_xerostatus");
                    query.Criteria.AddCondition("illumina_xeroid", ConditionOperator.Equal, (object)newEvent.ResourceId.ToString());
                    WebProxyClient webProxyClient = new WebProxyClient();
                    EntityCollection entityCollection = webProxyClient.RetrieveMultiple((QueryBase)query);
                    if (entityCollection.Entities.Count == 1)
                    {
                        Entity entity1 = entityCollection.Entities.FirstOrDefault<Entity>();
                        Entity entity2 = new Entity("illumina_purchaseorder", entity1.Id);
                        Decimal valueOrDefault1 = invoice.AmountCredited.GetValueOrDefault();
                        Money attributeValue1 = entity1.GetAttributeValue<Money>("illumina_totalxerocredited");
                        Decimal num1 = attributeValue1 != null ? attributeValue1.Value : 0M;
                        if (valueOrDefault1 != num1)
                            entity2["illumina_totalxerocredited"] = (object)new Money(invoice.AmountCredited.GetValueOrDefault());
                        Decimal? nullable = invoice.AmountPaid;
                        Decimal valueOrDefault2 = nullable.GetValueOrDefault();
                        Money attributeValue2 = entity1.GetAttributeValue<Money>("illumina_totalxeropaid");
                        Decimal num2 = attributeValue2 != null ? attributeValue2.Value : 0M;
                        if (valueOrDefault2 != num2)
                        {
                            Entity entity3 = entity2;
                            nullable = invoice.AmountPaid;
                            Money money = new Money(nullable.GetValueOrDefault());
                            entity3["illumina_totalxeropaid"] = (object)money;
                        }
                        nullable = invoice.AmountDue;
                        Decimal valueOrDefault3 = nullable.GetValueOrDefault();
                        Money attributeValue3 = entity1.GetAttributeValue<Money>("illumina_outstandingbalance");
                        Decimal num3 = attributeValue3 != null ? attributeValue3.Value : 0M;
                        if (valueOrDefault3 != num3)
                        {
                            Entity entity4 = entity2;
                            nullable = invoice.AmountDue;
                            Money money = new Money(nullable.GetValueOrDefault());
                            entity4["illumina_outstandingbalance"] = (object)money;
                        }
                        if (invoice.Status != (InvoiceStatus)entity1.GetAttributeValue<OptionSetValue>("illumina_xerostatus").Value)
                            entity2["illumina_xerostatus"] = (object)new OptionSetValue((int)invoice.Status);
                        if (entity2.Attributes.Count > 0)
                        {
                            webProxyClient.Update(entity2);
                            log.Info("CRM Update Success");
                        }
                    }
                    else
                        log.Info("Found - " + entityCollection.Entities.Count.ToString() + " ?!?!");
                }
                log.Info("Ended Xero Process");
            }
            catch (RateExceededException ex)
            {
                log.Info("Xero has hit the limit for - " + ex.RateLimitProblem);
                log.Info("Going to sleep for 90 seconds");
                Thread.Sleep(90000);
                log.Info("Finished sleeping for 90 seconds - Now will throw exception");
                throw;
            }
            catch (Exception ex)
            {
                log.Info(ex.Message);
            }
        }
    }
}
