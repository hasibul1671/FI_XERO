using Illuminance.Commons;
using IlluminanceSolutions.Utility;
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
	public class ValidateApplication : PluginBase {

		[PluginMessage(PluginMessageAttribute.Messages.Update)]
		[ColumnSet(true)]
		public void Validate() {
			if (!entity.GetAttributeValue<bool>("illumina_validate"))
				return;

			CheckFundValues(entity);
			CheckDistributionPolicy(entity);
		}

		private void CheckFundValues(Entity triggeringApp) {
			if (triggeringApp.ContainsData("illumina_relatedfunddetail"))
            {
				EntityReference fdRef = triggeringApp.GetAttributeValue<EntityReference>("illumina_relatedfunddetail");
				Entity fundDetail = orgService.Retrieve(fdRef.LogicalName, fdRef.Id, new ColumnSet("illumina_budgetavailable", "illumina_pendingallowancebalance"));

				Entity appMoniker = new Entity(triggeringApp.LogicalName, triggeringApp.Id);

				decimal fundPending = (fundDetail.GetAttributeValue<Money>("illumina_pendingallowancebalance")?.Value ?? 0);
				tracingService.Trace($"Fund Pending = {fundPending.ToString("C")}");
				if (fundPending < 0)
				{
					tracingService.Trace($"Adding alert.");
					appMoniker["illumina_alert"] = $"Application will exceed member allowance by {(fundPending).ToString("C")}. Please check the related Fund Detail.";
				}
				else
				{
					if (triggeringApp.GetAttributeValue<string>("illumina_alert") != null)
					{
						tracingService.Trace($"Removing alert.");
						appMoniker["illumina_alert"] = "";
					}
				}

				orgService.Update(appMoniker);
			}
		}

		private void CheckDistributionPolicy(Entity triggeringApp) {
			EntityReference distPolicyRef = triggeringApp.GetAttributeValue<EntityReference>("illumina_applicationformtype");
			Entity distPolicy = orgService.Retrieve(distPolicyRef.LogicalName, distPolicyRef.Id, new ColumnSet("illumina_budgetleft"));

			decimal appTotal = orgService.RetrieveMultiple(new QueryExpression("illumina_fundapplication") {
				ColumnSet = new ColumnSet("illumina_requestedamount"),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
						new ConditionExpression("statecode", ConditionOperator.Equal, 0),	//Active
						new ConditionExpression("illumina_approved", ConditionOperator.Equal, false),
						new ConditionExpression("illumina_relatedfunddetail", ConditionOperator.Equal, distPolicyRef.Id)
					}
				}
			}).Entities.Select(i => i.GetAttributeValue<Money>("illumina_requestedamount")?.Value ?? 0).Sum();

			Entity appMoniker = new Entity(triggeringApp.LogicalName, triggeringApp.Id);

			decimal distRemaining = (distPolicy.GetAttributeValue<Money>("illumina_budgetleft")?.Value ?? 0);
			if (distRemaining < appTotal) {
				tracingService.Trace($"Adding alert.");
				appMoniker["illumina_alert"] = $"Application will exceed total allowance by {(appTotal - distRemaining).ToString("C")}. Please check the related Distribution Policy.";
			}
			
			orgService.Update(appMoniker);
		}
	}
}
