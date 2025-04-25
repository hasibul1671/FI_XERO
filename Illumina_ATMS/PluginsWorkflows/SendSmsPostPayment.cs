using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
	public class SendSmsPostPayment : CodeActivity {

		[Output("SMS Message")]
		[ReferenceTarget("posms_smsmessage")]
		public OutArgument<EntityReference> SmsMessage { get; set; }

		protected override void Execute(CodeActivityContext context) {
			IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
			IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
			IOrganizationService orgService = serviceFactory.CreateOrganizationService(workflowContext.InitiatingUserId);
			ITracingService tracingService = context.GetExtension<ITracingService>();

			tracingService.Trace($"{this.GetType()}: Execution start.");

			try {
				Entity triggeringPo = orgService.Retrieve(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId, 
                    new ColumnSet(
                        "illumina_fundapplicationid",
                        "illumina_supplierid",
                        "illumina_purchaseorderstatus",
                        "illumina_totalamount",
                        "illumina_totalxeropaid"));

                tracingService.Trace($"PO({triggeringPo.Id}): Retrieved");

				EntityReference supplierRef = triggeringPo.GetAttributeValue<EntityReference>("illumina_supplierid");
				EntityReference appRef = triggeringPo.GetAttributeValue<EntityReference>("illumina_fundapplicationid");
			
				// Dont send if not closed
				if (triggeringPo.GetAttributeValue<OptionSetValue>("illumina_purchaseorderstatus").Value != 390950004)	// Closed
					throw new InvalidPluginExecutionException($"I'm not meant to send an SMS");

                if (triggeringPo.Contains("illumina_totalamount"))
                {
                    if (triggeringPo.GetAttributeValue<Money>("illumina_totalamount").Value == 0)
                    {
                        tracingService.Trace($"PO({triggeringPo.Id}): PO Total Amount is Zero - Ending Plugin");
                        throw new InvalidPluginExecutionException($"I'm not meant to send an SMS - PO Total Amount Zero");
                    }
                }

                if (triggeringPo.Contains("illumina_totalxeropaid"))
                {
                    if (triggeringPo.GetAttributeValue<Money>("illumina_totalxeropaid").Value <= 0)
                    {
                        tracingService.Trace($"PO({triggeringPo.Id}): PO Total Paid is Zero - Ending Plugin");
                        throw new InvalidPluginExecutionException($"I'm not meant to send an SMS - PO Total Paid Zero");
                    }
                }

				tracingService.Trace($"PO({triggeringPo.Id}): Sending SMS");

				Entity supplier = orgService.Retrieve(supplierRef.LogicalName, supplierRef.Id, new ColumnSet("illumina_taxinvoicerequired", "name"));

				// Dont send if tax invoice is NO
				if (!supplier.GetAttributeValue<bool>("illumina_taxinvoicerequired"))
					throw new InvalidPluginExecutionException($"I'm not meant to send an SMS here, I should have been sent upon creation.");

				tracingService.Trace($"PO({triggeringPo.Id}): Really Sending SMS");

				Entity app = orgService.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("illumina_contact", "illumina_relatedfunddetail"));

				EntityReference contactRef = app.GetAttributeValue<EntityReference>("illumina_contact");
                EntityReference fdRef = app.GetAttributeValue<EntityReference>("illumina_relatedfunddetail");

				Entity contact = orgService.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("mobilephone", "fullname"));
                Entity fundDetail = orgService.Retrieve(fdRef.LogicalName, fdRef.Id, new ColumnSet("illumina_budgetleft", "illumina_subcommunityprogram"));

				if (contact.GetAttributeValue<string>("mobilephone") == null)
					throw new InvalidPluginExecutionException($"Contact {contact.GetAttributeValue<string>("fullname")} does not have a mobile phone.");

				tracingService.Trace($"PO({triggeringPo.Id}): Really Really Sending SMS");

				Entity sms = new Entity("posms_smsmessage");
				sms["posms_phonenumber"] = contact.GetAttributeValue<string>("mobilephone");
				sms["regardingobjectid"] = app.ToEntityReference();
				sms["posms_smsmessagelarge"] = CreateMessage(supplierRef, appRef, RetrieveDisbursements(orgService, triggeringPo), fundDetail, orgService);

				sms.Id = orgService.Create(sms);

				SmsMessage.Set(context, sms.ToEntityReference());
			}
			catch (Exception e) {
				for (var ie = e.InnerException; ie != null; ie = ie.InnerException)
					tracingService?.Trace(e.GetType() + ": " + e.Message);
				throw new InvalidPluginExecutionException(e.GetType() + ": " + e.Message + Environment.NewLine + e.StackTrace);
			}
		}

		private string CreateMessage(EntityReference supplierRef, EntityReference appRef, List<Entity> disbursements, Entity fundDetail, IOrganizationService orgService) {
			string message =
				$"Re: App#{appRef.Name} {Environment.NewLine}" +
				$"A payment has been made to {supplierRef.Name} for the following: {Environment.NewLine}";

			foreach (Entity disbursement in disbursements) 
				message += $"  - {disbursement.GetAttributeValue<EntityReference>("illumina_distributionpolicyitem").Name} - {disbursement.GetAttributeValue<Money>("illumina_amount").Value.ToString("C")}  {Environment.NewLine}";

            message +=
                $"The remaining balance of your {fundDetail.GetAttributeValue<EntityReference>("illumina_subcommunityprogram").Name} is now {fundDetail.GetAttributeValue<Money>("illumina_budgetleft").Value.ToString("C")}. {Environment.NewLine}";

			message +=
				$"Please allow 1-3 business days for funds to be received by the store. {Environment.NewLine}" +
				$"Kind regards, BNTAC Member Services";

			return message;
		}

		private static List<Entity> RetrieveDisbursements(IOrganizationService orgService, Entity triggeringPo) {
			List<Entity> disbursements = orgService.RetrieveMultiple(new QueryExpression("illumina_lineitem") {
				ColumnSet = new ColumnSet("illumina_distributionpolicyitem", "illumina_amount", "illumina_supplier"),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
							new ConditionExpression("illumina_purchaseorderid", ConditionOperator.Equal, triggeringPo.Id),
                            new ConditionExpression("statecode", ConditionOperator.Equal, 0)    // Active
						}
				}
			}).Entities.ToList();

			return disbursements;
		}
	}
}
