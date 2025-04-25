using System;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;

using Microsoft.Xrm;

namespace IlluminanceSolutions {
	public class SyncSharedFunds : IPlugin {
		/*
			!!!!!!!!UNUSED!!!!!!


			Message:	Update
			Field:		BudgetLeft, BudgetUsed
			Entity:		SubCommunityProgram (illumina_applicationformtype)
			Details:	When the Budgets change, reflects the changes on another SubCommunityProgram record which shares its funds
		
			illumina_applicationformtype
			-illumina_sharedfunds
			-illumina_budgetused
			-illumina_budgetleft
		*/

		//declare
		IPluginExecutionContext context = null;
			ITracingService tracingService = null;
			IOrganizationServiceFactory serviceFactory = null;
			IOrganizationService service = null;
			Entity thisEntity = null;

		static bool isInSyncSharedFunds = false;

		public void Execute(IServiceProvider serviceProvider) {
			initializeMeta(serviceProvider);

			// stop looping
			if (isInSyncSharedFunds)
				return;
			isInSyncSharedFunds = true;

			try {
				thisEntity = getThisEntity();
				if (thisEntity == null)
					throw new InvalidPluginExecutionException("Entity to sync with was null.");

				if (thisEntity.LogicalName == "illumina_formtype") {
					//get the sharedFunds. If it doesn't share funds, end plugin silently
					EntityReference sharedFunds = thisEntity.GetAttributeValue<EntityReference>("illumina_sharedfunds");
					if (sharedFunds == null)
						throw new Exception("Shared funds == null");//return;

					tracingService.Trace(sharedFunds.ToString());       //??		//TODO: remove

					//get the entity's fields
					decimal thisBudgetUsed = (decimal)thisEntity.GetAttributeValue<Money>("illumina_budgetused").Value;
					//decimal thisBudgetLeft = (decimal)thisEntity.GetAttributeValue<Money>("illumina_budgetleft").Value;

					//get related entity and its fields
					Entity relatedEntity = service.Retrieve(sharedFunds.LogicalName, sharedFunds.Id, new ColumnSet("illumina_budgetused", "illumina_budgetleft"));
					decimal relatedBudgetUsed = (decimal)relatedEntity.GetAttributeValue<Money>("illumina_budgetused").Value;
					//decimal relatedBudgetLeft = (decimal)relatedEntity.GetAttributeValue<Money>("illumina_budgetleft").Value;
					tracingService.Trace("Related Entity gotten {0}, {1} ", sharedFunds.LogicalName, sharedFunds.Id);

					tracingService.Trace("Pre change");
					//check and update only if they're different. Recursion prevention. Add to the existing
					if (!checkEqual(thisBudgetUsed, relatedBudgetUsed)) {
						relatedEntity["illumina_budgetused"] = new Money(thisBudgetUsed);
						//thisEntity["illumina_budgetused"] = new Money(thisBudgetUsed + relatedBudgetUsed);

						//relatedEntity["illumina_budgetleft"] = new Money(thisBudgetLeft + (decimal)relatedEntity.GetAttributeValue<Money>("illumina_budgetleft").Value);		//wrong, let business rule handle
						service.Update(relatedEntity);

						//throw new Exception("Unequal values. Just updated.");		//TODO: remove
					}
					else return;    // throw new Exception("Equal values. Has been updated.");	//TODO: exit silently
				
				}
				else throw new Exception("Plugin triggered on wrong entity. Supposed to trigger on: " + thisEntity.LogicalName); //return;    //silently
			}
			catch (Exception ex)        //every other exception, big details
			{
				throw new InvalidPluginExecutionException(ex.GetType() + " occurred in " + this.GetType() + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, ex);
			}
			finally {
				isInSyncSharedFunds = false;
			}
		}

		//Initializes the metadata objects that is needed for CRM plugins. Works as side effects.
		public void initializeMeta(IServiceProvider serviceProvider) {
			//initialize
			context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
			tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService)); ;
			serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
			service = serviceFactory.CreateOrganizationService(null);
			//XrmServiceContext org = null;

			tracingService.Trace("Plugin {0} has started.", this.GetType().Name);
		}
	
		//TODO: remake this into more reusable with passing a single int param
		//checks what Image is available for the Plugin Step. Prioritizing postImage.
		public Entity getThisEntity() {
				if (context.PostEntityImages.Contains("postImage") && context.PostEntityImages["postImage"] is Entity) {
				tracingService.Trace("post");
				return thisEntity = (Entity)context.PostEntityImages["postImage"];
			}
			else if (context.PreEntityImages.Contains("preImage") && context.PreEntityImages["preImage"] is Entity) {
				tracingService.Trace("pre");
				return thisEntity = (Entity)context.PostEntityImages["preImage"];
			}
			else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity) {
				tracingService.Trace("target");
				return thisEntity = (Entity)context.InputParameters["Target"];
			}
			else {
				tracingService.Trace("Null");
				return null;
			}
		}

		//checks whether the two is equal
		public bool checkEqual(decimal thisBudget, decimal relatedBudget) {
			if (thisBudget == relatedBudget)
				return true;
			else return false;
		}
	}
}

