using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Activities;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;

namespace IlluminanceSolutions {
	/// <summary>
	/// Plugin Registration:
	///	  Messages: On-demand
	///	  Primary Entity: Contact (Member)
	///	  Filtering Attributes: *
	///	  State of Execution: Post-operation
	///	  Execution Mode: Synchronous
	///	  Configuration:
	///	  Functionality: On-demand, Recalc the Fund Details for a Contact
	/// </summary>
	/// 

	public sealed class RecalcFundDetails : CodeActivity {
		//initialize meta
		ITracingService tracingService;
		IWorkflowContext context;
		IOrganizationServiceFactory serviceFactory;
		IOrganizationService service;

		protected override void Execute(CodeActivityContext executionContext) {
			tracingService = executionContext.GetExtension<ITracingService>();
			context = executionContext.GetExtension<IWorkflowContext>();
			serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
			service = serviceFactory.CreateOrganizationService(context.UserId);

			//Entity thisContact = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, null);		//see if null columnSet works

			ColumnSet columnSet = new ColumnSet("illumina_budgetused");
			EntityCollection relatedFundDetails = getRelated("illumina_funddetails", columnSet, "illumina_contactid", ConditionOperator.Equal, context.PrimaryEntityId.ToString());

			foreach (var thisFundDetail in relatedFundDetails.Entities) {
				columnSet = new ColumnSet("illumina_requestedamount");
				EntityCollection relatedFundApplications = getRelated("illumina_fundapplication", columnSet, "illumina_relatedfunddetail", ConditionOperator.Equal, thisFundDetail.Id.ToString());

				decimal total = 0;
				foreach (var thisFundApplication in relatedFundApplications.Entities) {
					total += thisFundApplication.GetAttributeValue<Money>("illumina_requestedamount").Value;
				}
				thisFundDetail["illumina_budgetused"] = new Money(total);
				service.Update(thisFundDetail);

			}
		}


		//TODO: check if string works
		public EntityCollection getRelated(string entityName, ColumnSet columnSet, string attribute = null, ConditionOperator condition = ConditionOperator.Equal, string match = null) {
			QueryExpression q = new QueryExpression(entityName);
			q.ColumnSet = columnSet;
            q.Criteria.AddCondition(attribute, condition, match);

            if (entityName == "illumina_fundapplication")
                q.Criteria = new FilterExpression(LogicalOperator.And) {
                    Conditions = {
                        new ConditionExpression("statuscode", ConditionOperator.In, new int[] { 100000000, 1, 2 } ),	//Approved or Approved
                        new ConditionExpression("statuscode", ConditionOperator.NotEqual, 390950002),   // Draft
					    new ConditionExpression("illumina_approved", ConditionOperator.Equal, true),
                        new ConditionExpression(attribute, condition, match)
                    }
                };

			EntityCollection ec = service.RetrieveMultiple(q);

			tracingService.Trace("Retrieved " + entityName + ": " + ec.Entities.Count);
			return ec;
		}
	}
}
