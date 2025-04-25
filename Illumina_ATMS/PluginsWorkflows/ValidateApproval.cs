using Illuminance.Commons;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
	public class ValidateApproval : PluginBase {
		[XmlSettingAttribute("stopUnapproval")]
		public bool stopUnapproval = false;

        [XmlSettingAttribute("roleid")]
        public string roleGuid = string.Empty;

		public ValidateApproval(string u, string s) : base(u, s) { }

		[PluginMessage(PluginMessageAttribute.Messages.Update)]
		[ColumnSet("illumina_requestedamount", "createdby", "modifiedby", "illumina_approved", "illumina_contact")]
        public void ValidateAppApproval() {
			if (entity.GetAttributeValue<bool>("illumina_approved")) {
				tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Validating approval.");

				CheckApprovingMSO(entity);
				CheckAuthorisationAmount(entity);
				UpdateApprover(entity);
			}
			else if (stopUnapproval) {
				tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Check MSOs aren't trying anything.");
				Entity preimage = null;
				if (pluginContext.PreEntityImages.TryGetValue("PreImage", out preimage)) {
					if (preimage.GetAttributeValue<bool>("illumina_approved")) {
						List<Entity> roles = adminService.RetrieveMultiple(new QueryExpression("role") {
							ColumnSet = new ColumnSet("name"),
							LinkEntities = {
								new LinkEntity {
									LinkFromEntityName = "role",
									LinkFromAttributeName = "roleid",
									LinkToEntityName = "systemuserroles",
									LinkToAttributeName = "roleid",
									LinkCriteria = new FilterExpression(LogicalOperator.And) {
										Conditions = {
											new ConditionExpression("systemuserid", ConditionOperator.Equal, pluginContext.InitiatingUserId)
										}
									}
								}
							}
						}).Entities.ToList();

						string[] permittedRoles = new string[] { "BNTAC - Manager", "BNTAC - Team Leader", "BNTAC - Finance Officer" };

						if (!roles.Any(r => permittedRoles.Contains(r.GetAttributeValue<string>("name")))) {
							throw new UserException("This Fund Application has already been approved, you can not unapproved it.");
						}

					}
					else {
						// Not Approved > Not Approved :/
					}
				}
				else {
					throw new PluginConfigurationException("PreImage not setup on Fund Application step.");
				}

			}
		}

		private void CheckApprovingMSO(Entity app) {
            Entity preApp = pluginContext.PreEntityImages["PreImage"];
            EntityReference preModifiedBy = preApp.GetAttributeValue<EntityReference>("modifiedby");
			EntityReference modifiedByRef = app.GetAttributeValue<EntityReference>("modifiedby");

            //if (preModifiedBy.Id == modifiedByRef.Id)
            //    throw new InvalidPluginExecutionException("Applications cannot be approved by the MSO who previously modified it. Please seek approval from another MSO.");

            if (UserConflictingWithMember(app))
                throw new InvalidPluginExecutionException("You cannot approve applications for members with conflicting interests. Please seek approval from another MSO.");

			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: User can approve.");
		}

        private bool UserConflictingWithMember(Entity app) {
            EntityReference modifiedByRef = app.GetAttributeValue<EntityReference>("modifiedby");
            EntityReference member = app.GetAttributeValue<EntityReference>("illumina_contact");

            return orgService.RetrieveMultiple(new QueryExpression("connection") {
                Criteria = new FilterExpression(LogicalOperator.And) {
                    Conditions = {
                        new ConditionExpression("record1id", ConditionOperator.Equal, modifiedByRef.Id),
                        new ConditionExpression("record2id", ConditionOperator.Equal, member.Id),
                        new ConditionExpression("record1roleid", ConditionOperator.Equal, new Guid(roleGuid))
                    }
                },
            }).Entities.Count > 0;
        }

        private void CheckAuthorisationAmount(Entity app) {
			EntityReference modifiedByRef = app.GetAttributeValue<EntityReference>("modifiedby");

			List<Entity> approvalLimits = GetApprovalLimits(modifiedByRef.Id);

			if (approvalLimits.Count == 0)
				throw new InvalidPluginExecutionException("You are not authorised to approve this application. Please seek approval from the Team Leader.");

			if (approvalLimits.Select(i => i.GetAttributeValue<Money>("illumina_approvallimit")?.Value ?? 0).Max() < (app.GetAttributeValue<Money>("illumina_requestedamount")?.Value ?? 0))
				throw new InvalidPluginExecutionException("This application's requested amount is greater than your authorisation. Please seek approval from an officer of higher authority.");

			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: User can approve.");
		}

		private void UpdateApprover(Entity app) {
			EntityReference modifiedByRef = app.GetAttributeValue<EntityReference>("modifiedby");
			Entity updateMoniker = new Entity(app.LogicalName, app.Id);
			updateMoniker["illumina_approvedby"] = modifiedByRef;
			orgService.Update(updateMoniker);
		}

		private List<Entity> GetApprovalLimits(Guid userId) {
			List<Entity> approvalLimits = adminService.RetrieveMultiple(new QueryExpression("illumina_approvallimit") {
				ColumnSet = new ColumnSet("illumina_securityrole", "illumina_approvallimit"),
				LinkEntities = {
					new LinkEntity {
						LinkFromEntityName = "illumina_approvallimit",
						LinkFromAttributeName = "illumina_securityrole",
						LinkToEntityName = "role",
						LinkToAttributeName = "roleid",
						LinkEntities = {
							new LinkEntity {
								LinkFromEntityName = "role",
								LinkFromAttributeName = "roleid",
								LinkToEntityName = "systemuserroles",
								LinkToAttributeName = "roleid",
								LinkCriteria = new FilterExpression(LogicalOperator.And) {
									Conditions = {
										new ConditionExpression("systemuserid", ConditionOperator.Equal, userId)
									}
								}
							}
						}
					}
				}
			}).Entities.ToList();

			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Retrieved {approvalLimits.Count} limit configurations.");

			return approvalLimits;
		}
	}
}
