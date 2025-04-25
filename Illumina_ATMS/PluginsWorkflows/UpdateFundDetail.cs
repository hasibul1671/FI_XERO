using Illuminance.Commons;
using IlluminanceSolutions.Utility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;

namespace IlluminanceSolutions
{
    public class UpdateFundDetail : PluginBase
    {
        [ColumnSet("illumina_relatedfunddetail", "illumina_disbursementscreated", "illumina_contact", "illumina_approved")]
        [PluginMessage(PluginMessageAttribute.Messages.Update)]
        public void OnApplicationUpdate()
        {
            RecalculateActualValues(entity);
            RecalculatePendingValues(entity);
        }

        [PluginEntitySource(PluginEntitySourceAttribute.EntitySource.PreImage, "PreImage")]
        [PluginMessage(PluginMessageAttribute.Messages.Delete)]
        public void OnApplicationDelete()
        {
            RecalculateActualValues(entity);
            RecalculatePendingValues(entity);
        }

        public void RecalculatePendingValues(Entity entity)
        {
            if (entity.ContainsData("illumina_relatedfunddetail"))
            {
                EntityReference fdRef = entity.GetAttributeValue<EntityReference>("illumina_relatedfunddetail");
                Entity fundDetail = orgService.Retrieve(fdRef.LogicalName, fdRef.Id, new ColumnSet("illumina_budgetleft", "illumina_budgetused", "statecode"));

                if (fundDetail.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                    return;

                decimal pendingTotal = orgService.RetrieveMultiple(new QueryExpression("illumina_fundapplication")
                {
                    ColumnSet = new ColumnSet("illumina_requestedamount"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions = {
                        new ConditionExpression("statecode", ConditionOperator.Equal, 0),	//Active
                        new ConditionExpression("statuscode", ConditionOperator.NotEqual, 390950002),   // Draft
						new ConditionExpression("illumina_approved", ConditionOperator.Equal, false),
                        new ConditionExpression("illumina_relatedfunddetail", ConditionOperator.Equal, fdRef.Id)
                    }
                    }
                }).Entities.Select(app => app.GetAttributeValue<Money>("illumina_requestedamount")?.Value ?? 0).Sum();

                CalcPendingAllowance(pendingTotal,
                    fundDetail.GetAttributeValue<Money>("illumina_budgetleft")?.Value ?? 0,
                    out decimal pendingBalance, out decimal pendingUsed);

                Entity fdMoniker = new Entity(fdRef.LogicalName, fdRef.Id);

                fdMoniker["illumina_pendingallowanceused"] = new Money(pendingUsed);
                fdMoniker["illumina_pendingallowancebalance"] = new Money(pendingBalance);

                orgService.Update(fdMoniker);
            }

        }

        internal void CalcPendingAllowance(decimal pendingTotal, decimal allowanceBalance, out decimal pendingBalance, out decimal pendingUsed)
        {
            tracingService.Trace($"Pending total = {pendingTotal}");
            tracingService.Trace($"Allowance balance = {allowanceBalance}");
            pendingUsed = pendingTotal;
            pendingBalance = allowanceBalance - pendingTotal;
        }

        public void RecalculateActualValues(Entity entity)
        {
            if (entity.ContainsData("illumina_relatedfunddetail"))
            {
                EntityReference fdRef = entity.GetAttributeValue<EntityReference>("illumina_relatedfunddetail");
                Entity fundDetail = orgService.Retrieve(fdRef.LogicalName, fdRef.Id, new ColumnSet("statecode"));

                if (fundDetail.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                    return;

                decimal actualTotal = orgService.RetrieveMultiple(new QueryExpression("illumina_fundapplication")
                {
                    ColumnSet = new ColumnSet("illumina_requestedamount"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions = {
                        new ConditionExpression("statuscode", ConditionOperator.In, new int[] { 100000000, 1, 2 } ),	//Approved or Approved
                        new ConditionExpression("statuscode", ConditionOperator.NotEqual, 390950002),   // Draft
						new ConditionExpression("illumina_approved", ConditionOperator.Equal, true),
                        new ConditionExpression("illumina_relatedfunddetail", ConditionOperator.Equal, fdRef.Id)
                    }
                    }
                }).Entities.Select(app => app.GetAttributeValue<Money>("illumina_requestedamount")?.Value ?? 0).Sum();

                tracingService.Trace($"Actual total = {actualTotal}");

                Entity fdMoniker = new Entity(fdRef.LogicalName, fdRef.Id);

                fdMoniker["illumina_budgetused"] = new Money(actualTotal);

                orgService.Update(fdMoniker);
            }
        }
    }
}
