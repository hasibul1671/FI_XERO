using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions.Utility {
    public static class ReCalcPO {
        public static decimal CaluculateValues(IOrganizationService orgService, ITracingService tracingService, string entityLogicalName, EntityReference poRef, string field) {
            tracingService.Trace("Calculating Field - " + field);
            return orgService.RetrieveMultiple(new QueryExpression(entityLogicalName) {
                ColumnSet = new ColumnSet(field, "illumina_quantity"),
                Criteria = new FilterExpression(LogicalOperator.And) {
                    Conditions = {
                        new ConditionExpression("illumina_purchaseorderid", ConditionOperator.Equal, poRef.Id),	// Under the same application
						new ConditionExpression("statecode", ConditionOperator.Equal, 0),	// Active
					}
                }
            }).Entities.Select(i => (i.GetAttributeValue<Money>(field)?.Value ?? 0)).Sum();
        }
    }
}
