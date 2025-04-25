using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IlluminanceSolutions.Utility;

namespace IlluminanceSolutions {
	public class PurchaseOrderRollup : IPlugin {
        public enum StatusCodes {
            New = 390950000,
            AwaitingDelivery = 390950001,
            SentToSupplier = 390950002,
            SendToSupplierFailed = 390950005,
            SendToXero = 390950003,
            SentToXeroUpdated = 390950006,
            SentToXeroError = 390950007,
            Closed = 390950004
        };
        public void Execute(IServiceProvider serviceProvider) {
            ITracingService tracingService = null;
            try {
                tracingService = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
                IPluginExecutionContext pluginContext = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
                IOrganizationServiceFactory serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
                IOrganizationService orgService = serviceFactory.CreateOrganizationService(pluginContext.UserId);

                tracingService.Trace("Custom Plugin {0} has started.", this.GetType().Name);

                Entity triggerEntity = orgService.Retrieve(pluginContext.PrimaryEntityName, pluginContext.PrimaryEntityId, new ColumnSet("illumina_purchaseorderid"));

                if (!triggerEntity.Contains("illumina_purchaseorderid")) {
                    tracingService.Trace("No associated Purchase Order, ending plugin");
                    return;
                }
                Entity po = orgService.Retrieve(triggerEntity.GetAttributeValue<EntityReference>("illumina_purchaseorderid").LogicalName, triggerEntity.GetAttributeValue<EntityReference>("illumina_purchaseorderid").Id,
                    new ColumnSet(
                        "illumina_purchaseorderstatus",
                        "illumina_totalxerocredited",
                        "illumina_totalxeropaid"));

                int poStatus = po.GetAttributeValue<OptionSetValue>("illumina_purchaseorderstatus").Value;

                if (poStatus == (int)StatusCodes.New || poStatus == (int)StatusCodes.AwaitingDelivery) {
                    tracingService.Trace("Purchase Order is either New or Awaiting Delivery. Not going to update PO.");
                    return;
                }

                if (poStatus == (int)StatusCodes.Closed) {
                    tracingService.Trace("Purchase Order is Closed. Not going to update PO.");
                    throw new InvalidPluginExecutionException("Associated Purchase Order has been Closed - You cannot update the Disbursement!");
                }

                if (po.GetAttributeValue<Money>("illumina_totalxerocredited").Value > 0 || po.GetAttributeValue<Money>("illumina_totalxeropaid").Value > 0) {
                    tracingService.Trace("Purchase Order has partial payment. Not going to update PO.");
                    throw new InvalidPluginExecutionException("Associated Purchase Order has a partial payment/credit- You cannot update the Disbursement!");
                }

                EntityReference poRef = po.ToEntityReference();

                decimal relatedPODetailTotal = ReCalcPO.CaluculateValues(orgService, tracingService, pluginContext.PrimaryEntityName, poRef, "illumina_unitprice");
                decimal relatedPOTax = ReCalcPO.CaluculateValues(orgService, tracingService, pluginContext.PrimaryEntityName, poRef, "illumina_tax");
                decimal relatedPOTotal = ReCalcPO.CaluculateValues(orgService, tracingService, pluginContext.PrimaryEntityName, poRef, "illumina_amount");

                tracingService.Trace(relatedPODetailTotal.ToString());
                tracingService.Trace(relatedPOTax.ToString());
                tracingService.Trace(relatedPOTotal.ToString());

                Entity appMoniker = new Entity(poRef.LogicalName, poRef.Id);
				appMoniker["illumina_detailamount"] = new Money(relatedPODetailTotal);
                appMoniker["illumina_totaltax"] = new Money(relatedPOTax);
                appMoniker["illumina_totalamount"] = new Money(relatedPOTotal);
                if (po.GetAttributeValue<OptionSetValue>("illumina_purchaseorderstatus").Value == 390950003) {// Sent to Xero
                    appMoniker["illumina_purchaseorderstatus"] = new OptionSetValue(390950006); // Sent to Xero - Update
                }
                orgService.Update(appMoniker);

			} catch (Exception e) {
				for (var ie = e.InnerException; ie != null; ie = ie.InnerException)
					tracingService?.Trace(e.GetType() + ": " + e.Message);
				// Insert Better Exception Handling
				throw new InvalidPluginExecutionException(e.GetType() + ": " + e.Message + Environment.NewLine + e.StackTrace);
			}
		}
	}
}
