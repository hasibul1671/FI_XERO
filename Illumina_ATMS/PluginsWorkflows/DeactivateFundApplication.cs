using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
	public class DeactivateFundApplication : IPlugin {
		public void Execute(IServiceProvider serviceProvider) {
			ITracingService tracingService = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
			try {
				IPluginExecutionContext pluginContext = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
				IOrganizationServiceFactory serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
				IOrganizationService orgService = serviceFactory.CreateOrganizationService(pluginContext.UserId);
				
				tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: Started");

				Entity triggeringPO = orgService.Retrieve(pluginContext.PrimaryEntityName, pluginContext.PrimaryEntityId, new ColumnSet("statecode", "illumina_purchaseorderstatus", "illumina_fundapplicationid"));
				EntityReference faRef = triggeringPO.GetAttributeValue<EntityReference>("illumina_fundapplicationid");

				if (faRef == null)
					throw new InvalidPluginExecutionException($"Purchase Order({triggeringPO.Id}): Fund Application is empty.");

				tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: Checking if FA is done.");

				bool isDone = orgService.RetrieveMultiple(new QueryExpression(triggeringPO.LogicalName) {
					ColumnSet = new ColumnSet("statecode"),
					Criteria = {
						Filters = {
							new FilterExpression(LogicalOperator.And) {
								Conditions = {
									new ConditionExpression("illumina_fundapplicationid", ConditionOperator.Equal, faRef.Id),
								}

							},
							new FilterExpression(LogicalOperator.Or) {
								Conditions = {
									new ConditionExpression("statecode", ConditionOperator.NotEqual, 1),	// Not Inactive
									new ConditionExpression("illumina_purchaseorderstatus", ConditionOperator.NotEqual, 390950004),		// Not CLosed
								}
							}
						}
					}
				}).Entities.Count == 0;

				tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: isDone = {isDone}");

				if (isDone) {
					tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: Deactivating FA");

					orgService.Execute(new SetStateRequest {
						State = new OptionSetValue(1),  // Inactive
						Status = new OptionSetValue(100000000),		// Approved
						EntityMoniker = faRef,
					});

					tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: Fund Application({faRef.Id}): Deactivated");
				}

				tracingService.Trace($"[{MethodBase.GetCurrentMethod().DeclaringType}]: Finished");
			} catch (Exception e) {
				string msg = "";

				for(Exception iex = e.InnerException; iex.InnerException != null; iex = iex.InnerException)
					msg += $"{iex.Message}: {iex.StackTrace} {Environment.NewLine}";
				

				throw new InvalidPluginExecutionException($"[{MethodBase.GetCurrentMethod().DeclaringType}]: {e.Message}: {msg}");
			}
		}
	}
}
