using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

/// <summary>
///		Pipeline:	POST 
///		Message:	Create/Update illumina_funddetails
///		Function:	Updates Distribution Policies
/// </summary>

namespace IlluminanceSolutions {
	public class RollupFundDetails : Illuminance.Commons.PluginBase {
		public override void ExecuteStep() {

			//get distributionPolicy to update
			tracingService.Trace("Entering...");

			Entity thisEntity = null;
			Entity previousDistributionPolicy = null;
			if(pluginContext.MessageName == "Create" ) {
				thisEntity = orgService.Retrieve(pluginContext.PrimaryEntityName, pluginContext.PrimaryEntityId, new ColumnSet("illumina_subcommunityprogram"));
			}
			else if(pluginContext.MessageName == "Update") {
				thisEntity = orgService.Retrieve(pluginContext.PrimaryEntityName, pluginContext.PrimaryEntityId, new ColumnSet("illumina_subcommunityprogram"));

				//get previous distribution policy if changed
				Entity previousFundDetail = (Entity) pluginContext.PreEntityImages["preImg"];
				if(previousFundDetail.Attributes.ContainsKey("illumina_subcommunityprogram")) {
					EntityReference previousDistributionPolicyER = previousFundDetail.GetAttributeValue<EntityReference>("illumina_subcommunityprogram");
					previousDistributionPolicy = orgService.Retrieve(previousDistributionPolicyER.LogicalName, previousDistributionPolicyER.Id, new ColumnSet("illumina_budgetleft", "illumina_maxbudget", "illumina_budgetused", "statecode"));
				}
			}
			else if(pluginContext.MessageName == "Delete") {
				tracingService.Trace("Entity - Delete");
				thisEntity = (Entity) pluginContext.PreEntityImages["preImg"];
			}

			EntityReference distributionPolicyER = thisEntity.GetAttributeValue<EntityReference>("illumina_subcommunityprogram");
			Entity distributionPolicy = orgService.Retrieve(distributionPolicyER.LogicalName, distributionPolicyER.Id, new ColumnSet("illumina_budgetleft", "illumina_maxbudget", "illumina_budgetused", "statecode"));

            if (distributionPolicy.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                return;

            UpdateDistributionPolicy(distributionPolicy);
			if(previousDistributionPolicy != null) {
                if (previousDistributionPolicy.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                    return;
                UpdateDistributionPolicy(previousDistributionPolicy);
			}

		}

		private void UpdateDistributionPolicy(Entity distributionPolicy) {
			//get all related Fund Details
			tracingService.Trace("Retrieving FundDetails");
			QueryExpression q = new QueryExpression("illumina_funddetails");
			q.Criteria.AddCondition("illumina_subcommunityprogram", ConditionOperator.Equal, distributionPolicy.Id);
			q.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
			q.ColumnSet = new ColumnSet("illumina_budgetused");
			EntityCollection fundDetailCollection = orgService.RetrieveMultiple(q);
			tracingService.Trace($"Retrieved {fundDetailCollection.Entities.Count} FundDetails");

			//add up the budget used in ea fund detail
			decimal totalUsed = 0;
			foreach(Entity fundDetail in fundDetailCollection.Entities) {
				totalUsed += fundDetail.GetAttributeValue<Money>("illumina_budgetused").Value;
			}

			//update distribution polucy
			decimal maxBudget = distributionPolicy.GetAttributeValue<Money>("illumina_maxbudget").Value;
			distributionPolicy["illumina_budgetused"] = new Money(totalUsed);
			distributionPolicy["illumina_budgetleft"] = new Money(maxBudget - totalUsed);
			tracingService.Trace($"Updating DP with Budget Used: {totalUsed}");
			tracingService.Trace($"Updating DP with Budget Left: {maxBudget - totalUsed}");

			adminService.Update(distributionPolicy);
			tracingService.Trace("Exiting...");
		}
	}
}
