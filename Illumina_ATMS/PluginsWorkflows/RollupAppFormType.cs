using System;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;

using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Query;

using Microsoft.Xrm;

namespace IlluminanceSolutions {
	/// <summary>
	/// Plugin Registration:
	///	  Messages: Update illumina_fundsprovided
	///	  Primary Entity: Fund Application
	///	  Filtering Attributes: *
	///	  State of Execution: Post-operation
	///	  Execution Mode: Synchronous
	///	  Configuration:
	///	  Functionality: Rolls up fund applications into distribution policies
	/// </summary>
	/// 

	public class RollupAppFormType : IPlugin {

		//TODO:
		//Change and make this part of the rollup calculator

		IPluginExecutionContext context = null;
		ITracingService tracingService = null;
		IOrganizationServiceFactory serviceFactory = null;
		IOrganizationService service = null;
		Entity thisCaseEntity = null;
		DateTime rolloverDate;

		public void Execute(IServiceProvider serviceProvider) {
			try {
				initializeMeta(serviceProvider);
				tracingService.Trace("Plugin {0} has started.", this.GetType().Name);

				thisCaseEntity = getThisEntity();

				/*
					illumina_fundapplication		
					-illumina_requestedamount				
					-illumina_applicationformtype			
					-illumina_fundsprovided					
				*/

				//retrieve info from case
				if (thisCaseEntity.LogicalName == "illumina_fundapplication") {
					tracingService.Trace("Case entered");

					//get info from form
					//Money requestedAmount = thisCaseEntity.GetAttributeValue<Money>("illumina_requestedamount");
					decimal requestedAmountDec = 0;
					string thisAppFormType = "";
					bool thisFundsProvided = false;

					try {
						requestedAmountDec = thisCaseEntity.GetAttributeValue<Money>("illumina_requestedamount").Value;
						thisAppFormType = thisCaseEntity.GetAttributeValue<EntityReference>("illumina_applicationformtype")?.Name;
						thisFundsProvided = thisCaseEntity.GetAttributeValue<bool>("illumina_fundsprovided");
						tracingService.Trace("Fields gotten. Funds provided is {0}", thisFundsProvided);
					}
					catch {
						throw new Exception("A required field was not filled in!");
					}

					//check if a certain field is ticked, if its not dont do anything
					if (thisFundsProvided == false) {
						tracingService.Trace("thisFundsProvided is false");
						return;
					}

					tracingService.Trace("Checking Form Type");


					//get the ref entity application form type that is related to this case
					EntityReference CaseApplicationFormType = thisCaseEntity.GetAttributeValue<EntityReference>("illumina_applicationformtype");
					Entity ApplicationFormTypeEntity = service.Retrieve(CaseApplicationFormType.LogicalName, CaseApplicationFormType.Id, new ColumnSet("illumina_budgetused", "illumina_maxbudget", "illumina_budgetforanindividual", "illumina_sharedfunds", "illumina_rolloverdate"));
					rolloverDate = ApplicationFormTypeEntity.GetAttributeValue<DateTime>("illumina_rolloverdate");
					EntityCollection ec = getRelatedCases(ApplicationFormTypeEntity);


					if (ec.Entities.Count < 0)
						throw new Exception("Entity Count < 0");
					decimal recalcMoneyDec = recalcCases(ec);


					//if the subcomm/aft has shared funds, money is recalculated for both it and its shared funds and added
					EntityReference syncedFundsER = ApplicationFormTypeEntity?.GetAttributeValue<EntityReference>("illumina_sharedfunds");  //illumina_sharedfunds
					if (syncedFundsER != null) {
						tracingService.Trace("Shared Funds available");
						Entity syncApplicationFormTypeEntity = service.Retrieve(syncedFundsER.LogicalName, syncedFundsER.Id, new ColumnSet("illumina_budgetused", "illumina_maxbudget"));
						EntityCollection ec2 = getRelatedCases(syncApplicationFormTypeEntity);

						if (ec2.Entities.Count < 0)
							throw new Exception("Entity Count < 0");

						decimal recalcMoneyDec2 = recalcCases(ec2);

						//set the subcomm program
						ApplicationFormTypeEntity["illumina_budgetused"] = new Money(recalcMoneyDec + recalcMoneyDec2);
						service.Update(ApplicationFormTypeEntity);
						syncApplicationFormTypeEntity["illumina_budgetused"] = new Money(recalcMoneyDec + recalcMoneyDec2);
						service.Update(syncApplicationFormTypeEntity);
					}
					else {
						//set the subcomm program
						tracingService.Trace("Shared Funds unavailable");
						ApplicationFormTypeEntity["illumina_budgetused"] = new Money(recalcMoneyDec);
						service.Update(ApplicationFormTypeEntity);
					}

					//rollup for child
					rollupChild();
				}

			}
			catch (Exception ex)        //every other exception, big details
			{
				throw new InvalidPluginExecutionException(ex.GetType() + " occurred in " + this.GetType() + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace, ex);
			}
		}

		//throw exception if exceeding max budget
		public void checkMax(decimal recalcMoney, decimal maxMoney) {                   //illumina_maxbudget
			tracingService.Trace("recalcMoney: {0} and maxMoney (Limit): {1}", recalcMoney, maxMoney);
			if (recalcMoney > maxMoney)
				throw new Exception("Exceeding max budget for this Application Form! SubCommunity Program has $" + (maxMoney - recalcMoney) + "left.");
		}
		public void checkIndividual(decimal newMoney, decimal individualMoney) {        //illumina_budgetforanindividual
			tracingService.Trace("NewMoney: {0} and IndividualMoney (Limit): {1}", newMoney, individualMoney);
			if (newMoney > individualMoney)
				throw new Exception("Exceeding individual limit for this Application Form! SubCommunity Program limits an individual to $" + individualMoney);
		}

		//Initializes the metadata objects that is needed for CRM plugins. Works as side effects.
		public void initializeMeta(IServiceProvider serviceProvider) {
			//initialize
			context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
			tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService)); ;
			serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
			service = serviceFactory.CreateOrganizationService(null);
		}

		//checks what Image is available for the Plugin Step. Prioritizing postImage.
		public Entity getThisEntity() {
			if (context.PostEntityImages.Contains("postImage") && context.PostEntityImages["postImage"] is Entity) {
				tracingService.Trace("post");
				return thisCaseEntity = (Entity)context.PostEntityImages["postImage"];
			}
			else if (context.PreEntityImages.Contains("preImage") && context.PreEntityImages["preImage"] is Entity) {
				tracingService.Trace("pre");
				return thisCaseEntity = (Entity)context.PostEntityImages["preImage"];
			}
			else if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity) {
				tracingService.Trace("target");
				return thisCaseEntity = (Entity)context.InputParameters["Target"];
			}
			else {
				tracingService.Trace("Null");
				return null;
			}
		}

		//get the related fund applications/cases and recalc 
		public EntityCollection getRelatedCases(Entity ApplicationFormTypeEntity) {
			QueryExpression query = new QueryExpression("illumina_fundapplication");
			query.ColumnSet.AddColumns("illumina_requestedamount");
			query.Criteria.AddCondition("illumina_applicationformtype", ConditionOperator.Equal, ApplicationFormTypeEntity.Id);
			query.Criteria.AddCondition("statuscode", ConditionOperator.Equal, 100000000);   //find any Completed ones that are of current rollup
			query.Criteria.AddCondition("illumina_fundsprovided", ConditionOperator.Equal, true);
			query.Criteria.AddCondition("new_completedon", ConditionOperator.OnOrBefore, rolloverDate);     //before the CP's rollup
			query.Criteria.AddCondition("new_completedon", ConditionOperator.OnOrAfter, rolloverDate.AddYears(-1));     //in that 365 days

			tracingService.Trace("In between dates : " + rolloverDate.AddYears(-1).ToShortDateString() + " and " + rolloverDate.ToShortDateString());

			EntityCollection ec = null;
			try {
				ec = service.RetrieveMultiple(query);
				tracingService.Trace("Entity Collection of {0} gotten", ec.TotalRecordCount);
				return ec;
			}
			catch {
				tracingService.Trace("Query fail");
				throw new Exception("No other records");
			}

		}

		//recalc the money and set it for the subcommunity program's budgetused
		public decimal recalcCases(EntityCollection ec) {
			decimal recalcMoneyDec = 0;
			if (ec.Entities.Count > 0) {
				tracingService.Trace("ec >0");

				foreach (Entity e in ec.Entities) {
					if (e.GetAttributeValue<Money>("illumina_requestedamount").Value == 0)
						continue;
					recalcMoneyDec += e.GetAttributeValue<Money>("illumina_requestedamount").Value;
					tracingService.Trace("+= " + recalcMoneyDec);
				}
				return recalcMoneyDec;
			}
			else return 0;
		}

		//rollup children's educations into their child records from Fund APplications
		public void rollupChild() {
			tracingService.Trace("Entered rollupChild Plugin");

			//TODO: not hard code this
			Guid preHighId = new Guid("6D01C1C9-CDC9-E711-A82A-000D3AE0E4EF");
			string preHighName = "Education Assistance (Pre-High School)";
			Guid highId = new Guid("6B01C1C9-CDC9-E711-A82A-000D3AE0E4EF");
			string highName = "Education Assistance (High School & Tertiary)";

			try {
				EntityReference thisChild = thisCaseEntity.GetAttributeValue<EntityReference>("illumina_child");
				EntityReference thisCommProg = thisCaseEntity.GetAttributeValue<EntityReference>("illumina_applicationformtype");

				if (thisChild != null) {
					Entity childEntity = service.Retrieve(thisChild.LogicalName, thisChild.Id, new ColumnSet("illumina_prehigheducationfunds", "illumina_highschooltertiaryeducationfunds"));

					//retrieve
					QueryExpression q = new QueryExpression("illumina_fundapplication");
					q.ColumnSet = new ColumnSet("illumina_requestedamount");
					q.Criteria.AddCondition("illumina_child", ConditionOperator.Equal, thisChild.Id);
					q.Criteria.AddCondition("illumina_applicationformtype", ConditionOperator.Equal, thisCommProg.Id);

					//add up total from Fund Applications
					tracingService.Trace("Retrieving the Fund Applications for this Child's CP");
					EntityCollection childFundApplications = service.RetrieveMultiple(q);
					tracingService.Trace("Retrieved " + childFundApplications.Entities.Count + " Fund Applications.");
					decimal total = 0;
					foreach (var fundApplication in childFundApplications.Entities) {
						total += fundApplication.GetAttributeValue<Money>("illumina_requestedamount").Value;
					}

					//push to child record
					tracingService.Trace("Pushing data to child record");
					if (thisCommProg.Id == preHighId) {
						childEntity["illumina_prehigheducationfunds"] = new Money(total);
						service.Update(childEntity);
					}
					else if (thisCommProg.Id == highId) {
						childEntity["illumina_highschooltertiaryeducationfunds"] = new Money(total);
						service.Update(childEntity);
					}
					else {      //TODO: optimize so that it checks before doing calcs
						tracingService.Trace("Somehow managed to use not Education CP");
					}
				}
				else {
					tracingService.Trace("No child");
					return;
				}
				tracingService.Trace("Exiting rollupChild Plugin");
			}
			catch (Exception ex) {
				tracingService.Trace(ex.GetType() + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
			}
		}
	}
}