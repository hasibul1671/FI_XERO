using Illuminance.Commons;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions
{
    public class RollupDisbursements : PluginBase
    {

        [PluginEntitySource(PluginEntitySourceAttribute.EntitySource.PreImage, "PreImage")]
        [PluginMessage(PluginMessageAttribute.Messages.Delete)]
        public void OnDelete()
        {
            RecalculateValues();
        }

        [ColumnSet("illumina_fundapplication", "illumina_amount", "illumina_quantity")]
        [PluginMessage(PluginMessageAttribute.Messages.Create, PluginMessageAttribute.Messages.Update)]
        public void OnCreateUpdate()
        {
            RecalculateValues();
        }

        public void RecalculateValues()
        {
            try
            {
                EntityReference appRef = entity.GetAttributeValue<EntityReference>("illumina_fundapplication");
                Entity app = orgService.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("statecode"));

                if (app.GetAttributeValue<OptionSetValue>("statecode").Value != 0)
                    return;

                decimal relatedDisbursementTotal = orgService.RetrieveMultiple(new QueryExpression(pluginContext.PrimaryEntityName)
                {
                    ColumnSet = new ColumnSet("illumina_amount", "illumina_quantity"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions = {
                        new ConditionExpression("illumina_fundapplication", ConditionOperator.Equal, appRef.Id),	// Under the same application
						new ConditionExpression("statecode", ConditionOperator.Equal, 0),	// Active
					}
                    }
                }).Entities.Select(i => (i.GetAttributeValue<Money>("illumina_amount")?.Value ?? 0)).Sum();

                Entity appMoniker = new Entity(appRef.LogicalName, appRef.Id);
                appMoniker["illumina_requestedamount"] = new Money(relatedDisbursementTotal);

                orgService.Update(appMoniker);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }

        }
    }
}
