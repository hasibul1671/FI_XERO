using Microsoft.Crm.Sdk.Messages;
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
    public class ActivateDisbursements : CodeActivity {
        protected override void Execute(CodeActivityContext context) {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            ITracingService tracingService = context.GetExtension<ITracingService>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(workflowContext.InitiatingUserId);

            try {
                Entity triggeringApplication = orgService.Retrieve(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId, new ColumnSet("statuscode"));

                EntityCollection disbursements = orgService.RetrieveMultiple(new QueryExpression("illumina_lineitem") {
                    ColumnSet = new ColumnSet("statuscode"),
                    Criteria = new FilterExpression(LogicalOperator.And) {
                        Conditions = {
                            new ConditionExpression("illumina_fundapplication", ConditionOperator.Equal, triggeringApplication.Id)
                        }
                    }
                });

                tracingService.Trace($"Retrieved {disbursements.Entities.Count} disbursements");

                if (disbursements.Entities.Count == 0)
                    tracingService.Trace($"No disbursements to activate. I hope i dont get submitted from portal.");
                else 
                    foreach(Entity disbursement in disbursements.Entities) {
                        if (disbursement.GetAttributeValue<OptionSetValue>("statuscode").Value != 1) 
                            orgService.Execute(new SetStateRequest() {
                                State = new OptionSetValue(0),  // Active
                                Status = new OptionSetValue(1), // Active
                                EntityMoniker = disbursement.ToEntityReference(),
                            });
                    }
            } catch (Exception e) {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
