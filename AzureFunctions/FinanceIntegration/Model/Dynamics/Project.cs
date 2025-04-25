using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace FinanceIntegration.Model.Dynamics
{

    public class Project
    {

        [JsonProperty("illumina_projectid")]
        public Guid Id { get; set; } = Guid.Empty;

        [JsonProperty("illumina_name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("illumina_description")]
        public OptionSetValue ProjectDescription { get; set; } = null;

        [JsonProperty("illumina_xeroprojectid")]
        public EntityReference XeroProjectId { get; set; }


        [JsonProperty("illumina_cost_base")]
        public EntityReference CostBase { get; set; } = null;
        [JsonProperty("illumina_sellprice")]
        public EntityReference SellPrice { get; set; } = null;
        [JsonProperty("illumina_cost")]
        public EntityReference Cost { get; set; } = null;

        [JsonProperty("statuscode")]
        public string StatusCode { get; set; } = string.Empty;

        [JsonProperty("initiatinguserid")]
        public Guid InitiatingUserId { get; set; } = Guid.Empty;

        public Project(Guid id, Guid userId)
        {
            if (id == Guid.Empty) throw new Exception("Invalid id.");
            WebProxyClient webProxyClient = new WebProxyClient();
            Entity ProjectData = webProxyClient.Retrieve("illumina_project", id, new ColumnSet(new string[] { "illumina_projectid" }));
            if (ProjectData != null)
            {
                Id = ProjectData.Id;
                InitiatingUserId = userId;
                if (ProjectData.Attributes.ContainsKey("illumina_name")) Name = (string)ProjectData.Attributes["illumina_name"];
                if (ProjectData.Attributes.ContainsKey("illumina_xeroprojectid")) XeroProjectId = (EntityReference)ProjectData.Attributes["illumina_xeroprojectid"];

                //if (purchaseOrder.Attributes.ContainsKey("illumina_fundapplicationid")) FundApplication = (EntityReference)purchaseOrder.Attributes["illumina_fundapplicationid"];
                //if (purchaseOrder.Attributes.ContainsKey("illumina_xerotrackingcodeid")) XeroTrackingCode = (EntityReference)purchaseOrder.Attributes["illumina_xerotrackingcodeid"];
                //if (purchaseOrder.Attributes.ContainsKey("illumina_purchaseorderexpiry")) POExpiry = (DateTime)purchaseOrder.Attributes["illumina_purchaseorderexpiry"];
                //if (purchaseOrder.Attributes.ContainsKey("illumina_xeroid")) XeroId = (string)purchaseOrder.Attributes["illumina_xeroid"];
            }
            else
            {
                throw new Exception("Invalid purchase order returned.");
            }
        }

        public static void Process(String projectId, TraceWriter log)
        {


            UpdateXeroId(projectId, new OptionSetValue(1122334455));


        }
        private static Guid ConvertToGuid(string input)
        {
            if (Guid.TryParse(input, out Guid result))
            {
                return result;
            }
            throw new ArgumentException($"Invalid GUID format: {input}");
        }



        private static void UpdateXeroId(String projectId, OptionSetValue optionSetValue)
        {
            Guid ID = ConvertToGuid(projectId);
            Entity poMoniker = new Entity("illumina_project", ID);
            poMoniker["illumina_xeroprojectid"] = optionSetValue;

            WebProxyClient orgSvc = new WebProxyClient();
            orgSvc.Update(poMoniker);

        }


    }


}



