using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm;
using Microsoft.Xrm;

namespace IlluminanceSolutions {
	/// <summary>
	/// Plugin Registration:
	///	  Messages: Create
	///	  Primary Entity: Contact (Member)
	///	  Filtering Attributes: *
	///	  State of Execution: Post-operation
	///	  Execution Mode: Synchronous
	///	  Configuration:
	///	  Functionality: Stops fundapplication from being made if criteria missing
	/// </summary>
	/// 
	class AutoCriteria : IPlugin {
		Entity thisContactEntity = null;
		Relationship relationship = null;
		IOrganizationService service = null;
		ITracingService tracingService = null;
		EntityReferenceCollection ercNeeded = new EntityReferenceCollection();

		public void Execute(IServiceProvider serviceProvider) {
			try {
				//initialize
				IPluginExecutionContext context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
				tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService)); ;
				IOrganizationServiceFactory serviceFactory = serviceProvider.GetService(typeof(IOrganizationServiceFactory)) as IOrganizationServiceFactory;
				service = serviceFactory.CreateOrganizationService(null);

				if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
					thisContactEntity = (Entity)context.InputParameters["Target"];

				int age = thisContactEntity.GetAttributeValue<int>("illumina_age");
				Relationship relationship = getRelationship();
				EntityReferenceCollection erc = getAllCriterias();
				ercNeeded = erc;

				tracingService.Trace("Relationship: " + relationship.SchemaName.ToString() + " " + relationship.PrimaryEntityRole.Value.ToString());

				//checkAge(age);
				service.Associate(thisContactEntity.LogicalName, thisContactEntity.Id, relationship, erc);
				service.Update(thisContactEntity);

			}
			catch (Exception ex) {
				throw new InvalidPluginExecutionException(ex.Message + "\r\n" + ex.StackTrace);
			}
		}

		//Associates "Over 14" and "60+"
		public EntityReference checkAge(int age) {
			if (age > 14) {
				//associate with Over 14
				//add/remove from ec

				return new EntityReference();
			}
			return new EntityReference();
		}

		public EntityReferenceCollection getAllCriterias() {
			EntityReferenceCollection erc = new EntityReferenceCollection();
			QueryExpression query = new QueryExpression("illumina_communityprogramcriteria");
			query.ColumnSet.AddColumns("illumina_name");

			EntityCollection ec = service.RetrieveMultiple(query);
			foreach (var e in ec.Entities) {
				erc.Add(e.ToEntityReference());
				tracingService.Trace(e.ToEntityReference().Name + " : " + e.ToEntityReference().Id);
			}
			return erc;
		}

		public Relationship getRelationship() {
			RetrieveRelationshipRequest rrr = new RetrieveRelationshipRequest() {
				Name = "illumina_illumina_communityprogramcriteria_contact",
				RetrieveAsIfPublished = true
			};
			var rrrResponse = (RetrieveRelationshipResponse)service.Execute(rrr);
			relationship = new Relationship(rrrResponse.RelationshipMetadata.SchemaName);
			return relationship;
		}
	}
}

//service.Create(new Entity("illumina_illumina_fundingthingo_contact") {
//	Attributes = {
//		{ "illumina_fundo", 0 },
//		{ "contactid", 1 }
//	}
//});

//AssociateRequest ar = new AssociateRequest() {
//	 Target = thisContactEntity.ToEntityReference(),
//	 RelatedEntities = new EntityReferenceCollection() { /* TODO: FIXME */ },
//	 Relationship = new Relationship(rrrrrrresponse.RelationshipMetadata.SchemaName)
//};
//var aresp = (AssociateResponse)service.Execute(ar);
