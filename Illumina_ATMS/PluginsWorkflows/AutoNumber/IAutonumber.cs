/**
 * Copyright (c) 2017 Illuminance Solutions Pty Ltd
 * Authors: Sebastian Southen
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace IlluminanceSolutions {
	/// <summary>
	/// https://msdn.microsoft.com/en-us/library/gg327941.aspx#bkmk_DatabaseTransactions
	/// 
	/// Plugin Registration:
	///	  Messages: Create, (optionally)Update
	///	  Primary Entity: *
	///	  Filtering Attributes: *
	///	  State of Execution: Pre-operation, Post-operation respectively
	///	  Execution Mode: Synchronous
	///	  Configuration: GUID of locking WorkflowAssistant entity instance
	/// </summary>
	public abstract class IAutonumber : IPlugin {
		// protected XmlDocument config;
		protected Entity lockEntity;
		protected ITracingService tracingService;
		protected IOrganizationService service;

		/// <summary>Constructor</summary>
		public IAutonumber(string unsecure, string secure) {
			// Deserialize config:
			lockEntity = new Entity("illumina_workflowassistant") {
				Id = new Guid(unsecure)		// Xrm.Page.data.entity.getId()
			};
		}

		/// <summary>Plugin</summary>
		public void Execute(IServiceProvider serviceProvider) {
			if (serviceProvider == null) {
				throw new ArgumentNullException("serviceProvider");
			}
			tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
			IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
			if ((context.Depth > 2) /*|| (context.ParentContext != null)*/) {    // Prevent recursion, but trigger on data imports
				return;
			}

			try {

				Assembly asm = Assembly.GetExecutingAssembly();
				//AssemblyTitleAttribute title = (AssemblyTitleAttribute)AssemblyTitleAttribute.GetCustomAttribute(asm, typeof(AssemblyTitleAttribute));
				AssemblyName name = new AssemblyName(asm.FullName);
				tracingService.Trace("{0} v{1}", name.Name /*title.Title*/, name.Version);
				tracingService.Trace("Execute {0} {1} {2} {3} {4}", context.Stage, context.MessageName, context.PrimaryEntityName, context.PrimaryEntityId, context.Depth);
				// context.IsolationMode = PluginAssemblyIsolationMode
				// context.Mode = SdkMessageProcessingStepMode
				//tracingService.Trace("{0} {1}", context.InputParameters, context.InputParameters["Target"]);

				if (context.InputParameters.Contains("Target") && (context.InputParameters["Target"] is Entity)) {
					// Obtain the target entity from the input parameters:
					Entity target = (Entity)context.InputParameters["Target"];
					Entity image = null;

					if (context.PostEntityImages.Count > 0)
						image = context.PostEntityImages.First().Value;
					else if (context.PreEntityImages.Count > 0)
						image = context.PreEntityImages.First().Value;
					else if (context.MessageName == "Create")
						image = target;
					else //if (entity == null)
						throw new Exception("Need either pre- or post-entity image for " + context.MessageName);

					//target = new Entity(target.LogicalName, target.Id);	//prevent sql error

					IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
					service = serviceFactory.CreateOrganizationService(context.UserId);

					string fetch = @"<fetch distinct='false' no-lock='false' mapping='logical' aggregate='true'>
					                   <entity name='{0}'>
						                 <attribute name='{1}' alias='result' aggregate='max' />
					                   </entity>
					                 </fetch>";
					//	<filter type='and'>
					//	  <condition attribute='' operator='eq' value='{2}' />
					//	</filter>

					string field = this.GetFieldString(target, image);
					if (field != null) {
						if (!context.IsInTransaction) {
							throw new Exception("Cannot lock record when not in transaction");
						}
						//lock (SyncLock) {
						long ticks = DateTime.UtcNow.Ticks;
						tracingService.Trace("Locking on {0}", ticks);
						lockEntity.Attributes.Add("importsequencenumber", ticks);
						service.Update(lockEntity);     // http://stackoverflow.com/a/26187519
						//System.Threading.Thread.Sleep(100);

						//return ((int?)arg_78_0.GetAggregateValue(metadata.LogicalName, numberField, "max", filter)).GetValueOrDefault() + 1;
						EntityCollection entityCollection = service.RetrieveMultiple(new FetchExpression(string.Format(fetch, target.LogicalName, field)));
						if ((entityCollection.Entities.Count == 1) && entityCollection[0].Contains("result")) {
							int result = 0;
							if (entityCollection[0].GetAttributeValue<AliasedValue>("result").Value != null) {
								result = (int)(entityCollection[0].GetAttributeValue<AliasedValue>("result").Value);
							}
							tracingService.Trace("Aggregate max: {0} => {1}", result, result + 1);
							target[field] = result + 1;		//TODO: Concat with something if you want special

							this.PostNumberingCallback(target, image, target.GetAttributeValue<int>(field));

							if (context.Stage == 40) {
								//prevent sql error
								if(target.Attributes.ContainsKey("emailaddress1")) {
									target.Attributes.Remove("emailaddress1");
								}

								service.Update(target);
								tracingService.Trace("Updated");
							}
						}
						else {
							throw new Exception("Unable to aquire aggregate maximum of " + field + " from " + target.LogicalName + "s");
						}

						//	service.Update(lockEntity);
						//}
					}
					else {
						tracingService.Trace("Conditions not met, field name is null");
					}
					this.PostAlwaysCallback(target, image);
					if (context.Stage == 40) {
						//prevent sql error
						if(target.Attributes.ContainsKey("emailaddress1")) {
							target.Attributes.Remove("emailaddress1");
						}

						service.Update(target);
						tracingService.Trace("Updated");
					}
				}
			}
			// http://www.thomaslevesque.com/2015/06/21/exception-filters-in-c-6/
			catch (Exception e) when (!(e is InvalidPluginExecutionException)) {
				tracingService.Trace(e.GetType().ToString() + ": " + e.Message);
				for (Exception inner = e.InnerException; inner != null; inner = inner.InnerException) {
					tracingService.Trace("INNER: " + inner.GetType().ToString() + ": " + inner.Message);
				}
				tracingService.Trace(e.StackTrace);
				throw new InvalidPluginExecutionException(e.GetType().ToString() + ": " + e.Message, e);
			}
			finally {
				tracingService.Trace("Exiting");
			}
		}

		/// <summary>Conditional execution must be implemented in child implementations.</summary>
		/// <returns>The autonumber'd attribute, or null if the conditions for autonumbering are not met.</returns>
		public abstract string GetFieldString(Entity target, Entity image);

		/// <summary>Additional changes, if number was set/updated.</summary>
		public virtual void PostNumberingCallback(Entity target, Entity image, int number) { }

		/// <summary>Additional changes, regardless of number.</summary>
		public virtual void PostAlwaysCallback(Entity target, Entity image) { }
	}
}