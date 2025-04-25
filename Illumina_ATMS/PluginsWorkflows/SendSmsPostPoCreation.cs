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
	public class SendSmsPostPoCreation : CodeActivity {

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
				Entity triggeringApp = orgService.Retrieve(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId, new ColumnSet("illumina_applicationid", "illumina_contact", "illumina_approved", "illumina_relatedfunddetail"));

				tracingService.Trace($"App({triggeringApp.GetAttributeValue<int>("illumina_applicationid")}): Retrieved");

				if (!triggeringApp.GetAttributeValue<bool>("illumina_approved"))
					throw new InvalidPluginExecutionException($"I'm not meant to send an SMS");

				EntityReference contactRef = triggeringApp.GetAttributeValue<EntityReference>("illumina_contact");
                EntityReference fdRef = triggeringApp.GetAttributeValue<EntityReference>("illumina_relatedfunddetail");

                Entity contact = orgService.Retrieve(contactRef.LogicalName, contactRef.Id, new ColumnSet("mobilephone", "fullname"));
                Entity fundDetail = orgService.Retrieve(fdRef.LogicalName, fdRef.Id, new ColumnSet("illumina_budgetleft", "illumina_subcommunityprogram"));

                if (contact.GetAttributeValue<string>("mobilephone") == null)
					throw new InvalidPluginExecutionException($"Contact {contact.GetAttributeValue<string>("fullname")} does not have a mobile phone.");

				List<Entity> disbursements = RetrieveDisbursements(orgService, triggeringApp);

				// Check if there are no suppliers with Tax invoice required of NO
				if (!ValidateDisbursements(orgService, disbursements))
					throw new InvalidPluginExecutionException($"I'm not meant to send an SMS, no suppliers with tax invoice required = NO");

				if (!ValidatePOs(orgService, triggeringApp))
					throw new InvalidPluginExecutionException($"I'm not meant to send an SMS, no POs have been generated.");

				Entity sms = new Entity("posms_smsmessage");
				sms["posms_phonenumber"] = contact.GetAttributeValue<string>("mobilephone");
				sms["regardingobjectid"] = triggeringApp.ToEntityReference();
				sms["posms_smsmessagelarge"] = CreateMessage(triggeringApp, disbursements, fundDetail, orgService, tracingService);

				sms.Id = orgService.Create(sms);

				SmsMessage.Set(context, sms.ToEntityReference());
			}
			catch (Exception e) {
				for (var ie = e.InnerException; ie != null; ie = ie.InnerException)
					tracingService?.Trace(e.GetType() + ": " + e.Message);
				throw new InvalidPluginExecutionException(e.GetType() + ": " + e.Message + Environment.NewLine + e.StackTrace);
			}
		}

		private bool ValidatePOs(IOrganizationService orgService, Entity triggeringApp) {
			return orgService.RetrieveMultiple(new QueryExpression("illumina_purchaseorder") {
				ColumnSet = new ColumnSet("illumina_purchaseorderid"),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
						new ConditionExpression("illumina_fundapplicationid", ConditionOperator.Equal, triggeringApp.Id)
					}
				}
			}).Entities.Count > 0;
		}

		private bool ValidateDisbursements(IOrganizationService orgService, List<Entity> disbursements) {
			int count = 0;

			// Check POs' suppliers if any of them have taxinvoicerequired = NO, otherwise dont send anything
			foreach (Entity disbursement in disbursements) {
				EntityReference supplierRef = disbursement.GetAttributeValue<EntityReference>("illumina_supplier");
				Entity supplier = orgService.Retrieve(supplierRef.LogicalName, supplierRef.Id, new ColumnSet("illumina_taxinvoicerequired"));

				if (!supplier.GetAttributeValue<bool>("illumina_taxinvoicerequired"))
					count++;
			}

			return count > 0;
		}

		private string CreateMessage(Entity triggeringApp, List<Entity> disbursements, Entity fundDetail, IOrganizationService orgService, ITracingService tracingService) {
			string message = 
				$"Re: App#{triggeringApp.GetAttributeValue<int>("illumina_applicationid")} {Environment.NewLine}" +
				$"Your application has been processed and a purchase order / payment has been made as per below. {Environment.NewLine}";

            tracingService.Trace($"Message started");

			foreach (Entity disbursement in disbursements) {
				EntityReference supplierRef = disbursement.GetAttributeValue<EntityReference>("illumina_supplier");
				Entity supplier = orgService.Retrieve(supplierRef.LogicalName, supplierRef.Id, new ColumnSet("illumina_taxinvoicerequired", "name"));

                tracingService.Trace($"Supplier({supplier.Id}) retrieved.");

				EntityReference poRef = disbursement.GetAttributeValue<EntityReference>("illumina_purchaseorderid");
				Entity po = orgService.Retrieve(poRef.LogicalName, poRef.Id, new ColumnSet("illumina_purchaseorderstatus"));

                tracingService.Trace($"PurchaseOrder({po.Id}) retrieved.");

				if (!supplier.GetAttributeValue<bool>("illumina_taxinvoicerequired") && !new int[] { 390950005, 390950007 }.Contains(po.GetAttributeValue<OptionSetValue>("illumina_purchaseorderstatus").Value))	// !Sent to Supplier - Error AND !Sent to Xero - Error
					message += $"  - {supplier.GetAttributeValue<string>("name")} - {disbursement.GetAttributeValue<Money>("illumina_amount").Value.ToString("C")} - {disbursement.GetAttributeValue<EntityReference>("illumina_distributionpolicyitem").Name} {Environment.NewLine}";

                tracingService.Trace($"Disbursement({disbursement.Id}) added to message");
            }

			message += 
				$"If you need to collect goods from the store, please visit the service desk at the store during office hours(9am to 4pm, Mon - Fri). {Environment.NewLine}" +
                $"{Environment.NewLine}" +
                $"The remaining balance of your {fundDetail.GetAttributeValue<EntityReference>("illumina_subcommunityprogram").Name} is now {fundDetail.GetAttributeValue<Money>("illumina_budgetleft").Value.ToString("C")}. {Environment.NewLine}" +
                $"Supply of grocery/fuel gift cards are subject to availability at the store you are collecting from. {Environment.NewLine}" +
                $"Purchase orders for collection of fuel at stores / service stations are restricted to FUEL ONLY.No other items are included. {Environment.NewLine}" +
				$"Kind regards, BNTAC Member Services";

			return message;
		}

		private static List<Entity> RetrieveDisbursements(IOrganizationService orgService, Entity triggeringApp) {
			List<Entity> disbursements = orgService.RetrieveMultiple(new QueryExpression("illumina_lineitem") {
				ColumnSet = new ColumnSet("illumina_distributionpolicyitem", "illumina_amount", "illumina_supplier", "illumina_purchaseorderid"),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
							new ConditionExpression("illumina_fundapplication", ConditionOperator.Equal, triggeringApp.Id),
                            new ConditionExpression("statecode", ConditionOperator.Equal, 0)    // Active
						}
				}
			}).Entities.ToList();

			return disbursements;
		}
	}
}
