// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroInvoice
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xero.Api.Core.Model;
using Xero.Api.Core.Model.Status;
using Xero.Api.Core.Model.Types;
using XeroLibApiCoreModel = Xero.Api.Core.Model;

namespace FinanceIntegration.Model.Xero
{
    public static class XeroInvoice
    {
        public static Invoice BuildInvoice(
          FinanceIntegration.Model.Dynamics.PurchaseOrder purchaseOrder,
          Contact xeroContact,
          List<LineItem> lineItems,
          EntityCollection disbursementEC,
          TraceWriter log)
        {
            log.Info("Entered - BuildInvoice - Building JSON for API call");
            string str = purchaseOrder.Name;
            WebProxyClient webProxyClient = new WebProxyClient();
            if (disbursementEC.Entities.Count > 0)
            {
                EntityReference entityReference = (EntityReference)disbursementEC.Entities.FirstOrDefault<Entity>()["illumina_distributionpolicy"];
                EntityReference attributeValue = webProxyClient.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(new string[2]
                {
                      "illumina_name",
                      "illumina_subfund"
                })).GetAttributeValue<EntityReference>("illumina_subfund");
                Entity entity = webProxyClient.Retrieve(attributeValue.LogicalName, attributeValue.Id, new ColumnSet(new string[1]
                {
                    "illumina_name"
                }));
                if (entity != null)
                    str = entity.GetAttributeValue<string>("illumina_name") + " / " + str;
            }
            Invoice invoice1 = new Invoice();
            invoice1.Contact = xeroContact;
            invoice1.LineItems = lineItems;
            invoice1.Type = InvoiceType.AccountsPayable;
            invoice1.LineAmountTypes = LineAmountType.Exclusive;
            DateTime dateTime = DateTime.Now;
            dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Utc);
            invoice1.DueDate = new DateTime?(dateTime.AddDays(7.0));
            invoice1.Reference = str;
            invoice1.Number = str;
            Invoice invoice2 = invoice1;
            Entity entity1 = webProxyClient.Retrieve("illumina_purchaseorder", purchaseOrder.Id, new ColumnSet(new string[2]
            {
        "illumina_xeroid",
        "illumina_xerostatus"
            }));
            if (entity1.Contains("illumina_xeroid"))
            {
                log.Info("XeroId was not null therefore is an update call");
                invoice2.Id = Guid.Parse(entity1.GetAttributeValue<string>("illumina_xeroid"));
                invoice2.Status = (InvoiceStatus)entity1.GetAttributeValue<OptionSetValue>("illumina_xerostatus").Value;
            }
            else
            {
                log.Info("XeroId not found therefore create call");
                invoice2.Status = InvoiceStatus.Submitted;
            }
            log.Info("Exiting - BuildInvoice - JSON created");
            return invoice2;
        }

        // Parse invoice status to string
        public static string ParseInvoiceStatus(XeroLibApiCoreModel.Status.InvoiceStatus invoiceStatus)
        {
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Draft)
                return "DRAFT";
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Deleted)
                return "DELETED";
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Submitted)
                return "SUBMITTED";
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Paid)
                return "PAID";
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Authorised)
                return "AUTHORISED";
            if (invoiceStatus == XeroLibApiCoreModel.Status.InvoiceStatus.Voided)
                return "VOIDED";
            //default: 
            return "DRAFT";
        }
        // Parse invoice type to string
        public static string ParseInvoiceType(XeroLibApiCoreModel.Types.InvoiceType invoiceType)
        {
            if (invoiceType == InvoiceType.AccountsPayable)
                return "ACCPAY";
            if (invoiceType == InvoiceType.AccountsReceivable)
                return "ACCREC";
            //default:
            return "ACCPAY";
        }
        // Parse line item type to string
        public static string ParseLineAmountType(XeroLibApiCoreModel.Types.LineAmountType lineAmountType)
        {
            if (lineAmountType == XeroLibApiCoreModel.Types.LineAmountType.NoTax)
                return "NoTax";
            if (lineAmountType == XeroLibApiCoreModel.Types.LineAmountType.Exclusive)
                return "Exclusive";
            if (lineAmountType == XeroLibApiCoreModel.Types.LineAmountType.Inclusive)
                return "Inclusive";
            //default:
            return "Exclusive";
        }   
    }
}
