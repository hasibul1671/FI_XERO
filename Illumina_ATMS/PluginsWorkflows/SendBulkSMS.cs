using Illuminance.Commons;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
	public class SendBulkSMS : CodeActivityBase {
		enum ListTargetTypes { Contact = 2, Account = 1, Lead = 4 };
		static Dictionary<string, string> tokens = new Dictionary<string, string>() {
			{"Full Name", "fullname" },
			{"First Name", "firstname" },
			{"Last Name", "lastname" }
		};

		protected override void ExecuteBody() {
			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Execution start.");

			try {
				#region Retrieve required data
				Entity bulkJob = orgService.Retrieve(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId, new ColumnSet("illumina_smstemplate", "illumina_targetlist"));

				EntityReference templateRef = bulkJob.GetAttributeValue<EntityReference>("illumina_smstemplate");
				EntityReference listRef = bulkJob.GetAttributeValue<EntityReference>("illumina_targetlist");

				Entity template = orgService.Retrieve(templateRef.LogicalName, templateRef.Id, new ColumnSet("illumina_templatemessage"));
				Entity targetList = orgService.Retrieve(listRef.LogicalName, listRef.Id, new ColumnSet("type", "query", "createdfromcode"));

				string templateMessage = template.GetAttributeValue<string>("illumina_templatemessage");
				List<Entity> listMembers = EvaluateMembers(targetList);
				tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Retrieved {listMembers.Count} contacts from target list");
				#endregion

				foreach (Entity member in listMembers) {
					tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Contact({member.Id}) = Start.");

					Entity smsMessage = new Entity("posms_smsmessage");
					smsMessage["posms_phonenumber"] = member.GetAttributeValue<string>("mobilephone");
					smsMessage["posms_smsmessagelarge"] = BuildMessage(templateMessage, member);
					smsMessage["regardingobjectid"] = member.ToEntityReference();
					
					Guid smsId = orgService.Create(smsMessage);
					tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Contact({member.Id}) = SMS Created.");

					SendSMS(smsId);
					tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Contact({member.Id}) = SMS Pending Send.");
				}
			}
			catch (InvalidPluginExecutionException) {
				throw;
			} catch (Exception ex) {
				var msg = "";
				for (var ie = ex; ie != null; ie = ie.InnerException)
					msg += ie.GetType() + ": " + ie.Message + Environment.NewLine;

				msg += ex.StackTrace;

				throw new InvalidPluginExecutionException(msg);
			}

			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Execution finished.");
		}

		private string BuildMessage(string templateMessage, Entity contact) {
			Regex regex = new Regex("\\[(.*?)\\]");
			IEnumerable<string> matches = regex.Matches(templateMessage).Cast<Match>().Select(m => m.Groups[1].Value);
			
			foreach(string match in matches) {
				if (tokens.ContainsKey(match) && contact.Attributes.ContainsKey(tokens[match]))
					templateMessage = templateMessage.Replace($"[{match}]", contact[tokens[match]].ToString());
				else if (contact.Attributes.ContainsKey(match))
					templateMessage = templateMessage.Replace($"[{match}]", contact[match].ToString());
				else
					templateMessage = templateMessage.Replace($"[{match}]", "");
			}

			return templateMessage;
		}

		private void SendSMS(Guid smsId) {
			orgService.Execute(new SetStateRequest() {
				EntityMoniker = new EntityReference("posms_smsmessage", smsId),
				State = new OptionSetValue(1),				// Completed
				Status = new OptionSetValue(730000002),		// Pending Sent
			});
		}

		private List<Entity> EvaluateMembers(Entity targetList) {
			bool isDynamic = targetList.GetAttributeValue<bool>("type");
			string query = targetList.GetAttributeValue<string>("query");

			if (isDynamic && query.Length > 0)
				return EvaluateDynamicMembers(query);
			else
				return EvaluateStaticMembers(targetList);
		}

		private List<Entity> EvaluateDynamicMembers(string query) {
			return orgService.RetrieveMultiple(new FetchExpression(query)).Entities.ToList();
		}

		private List<Entity> EvaluateStaticMembers(Entity targetList) {
			List<Guid> listMembers = orgService.RetrieveMultiple(new QueryExpression("listmember") {
				ColumnSet = new ColumnSet("entityid"),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
						new ConditionExpression("listid", ConditionOperator.Equal, targetList.Id)
					}
				}
			}).Entities.Select(e => e.GetAttributeValue<EntityReference>("entityid").Id).ToList();

			tracingService.Trace($"{MethodBase.GetCurrentMethod().DeclaringType}: Retrieved {listMembers.Count} members from target list");

			return orgService.RetrieveMultiple(new QueryExpression("contact") {
				ColumnSet = new ColumnSet(true),
				Criteria = new FilterExpression(LogicalOperator.And) {
					Conditions = {
						new ConditionExpression("contactid", ConditionOperator.In, listMembers)
					}
				}
			}).Entities.ToList();
		}
	}
}
