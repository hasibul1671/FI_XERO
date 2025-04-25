using Illuminance.Commons;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
    public class ConflictingInterest : PluginBase {
        [XmlSettingAttribute("roleid")]
        public string roleid = string.Empty;

        public ConflictingInterest(string u, string s) : base(u, s) { }

        // Against "illumina_fundapplicationprocess"
        [PluginMessage(PluginMessageAttribute.Messages.Update)]
        [ColumnSet("activestageid", "bpf_illumina_fundapplicationid", "modifiedby")]
        public void CheckConflict() {
            string stageName = entity.GetAttributeValue<EntityReference>("activestageid").Name;

            if (new string[] { "Documents", "Validation", "Approval" }.Contains(stageName)) {
                EntityReference appRef = entity.GetAttributeValue<EntityReference>("bpf_illumina_fundapplicationid");
                EntityReference modifiedByRef = entity.GetAttributeValue<EntityReference>("modifiedby");
                Entity app = orgService.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("illumina_contact"));
                EntityReference member = app.GetAttributeValue<EntityReference>("illumina_contact");

                CheckConnection(modifiedByRef, member);
            }
        }

        private void CheckConnection(EntityReference user, EntityReference member) {
            bool conflicting = orgService.RetrieveMultiple(new QueryExpression("connection") {
                Criteria = new FilterExpression(LogicalOperator.And) {
                    Conditions = {
                            new ConditionExpression("record1id", ConditionOperator.Equal, user.Id),
                            new ConditionExpression("record2id", ConditionOperator.Equal, member.Id),
                            new ConditionExpression("record1roleid", ConditionOperator.Equal, new Guid(roleid)),
                            new ConditionExpression("statecode", ConditionOperator.Equal, 0) // Active
                        }
                },
            }).Entities.Count > 0;

            if (conflicting)
                throw new InvalidPluginExecutionException("You cannot process applications for members with conflicting interests. Please seek another MSO to process this application further.");
        }

        //Against disbursements
        [PluginMessage(PluginMessageAttribute.Messages.Create)]
        [PluginEntitySource(PluginEntitySourceAttribute.EntitySource.Target)]
        public void CheckConflictDisbursement() {
            EntityReference appRef = entity.GetAttributeValue<EntityReference>("illumina_fundapplication");
            EntityReference createdBy = entity.GetAttributeValue<EntityReference>("createdby");
            Entity app = orgService.Retrieve(appRef.LogicalName, appRef.Id, new ColumnSet("illumina_contact"));
            EntityReference member = app.GetAttributeValue<EntityReference>("illumina_contact");

            CheckConnection(createdBy, member);
        }
    }
}
