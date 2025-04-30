// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.PurchaseOrder
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FinanceIntegration.Model.Dynamics
{
    public class PurchaseOrder
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.Empty;

        [JsonProperty("illumina_name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("illumina_purchaseorderstatus")]
        public OptionSetValue PurchaseOrderStatus { get; set; }

        [JsonProperty("illumina_supplierid")]
        public EntityReference Supplier { get; set; }

        [JsonProperty("illumina_fundapplicationid")]
        public EntityReference FundApplication { get; set; }

        [JsonProperty("illumina_xerotrackingcodeid")]
        public EntityReference XeroTrackingCode { get; set; }

        [JsonProperty("illumina_xeroid")]
        public string XeroId { get; set; } = string.Empty;

        [JsonProperty("initiatinguserid")]
        public Guid InitiatingUserId { get; set; } = Guid.Empty;

        public static List<string> FailureDescription { get; set; } = new List<string>();

        public static bool Success { get; set; } = false;

        public PurchaseOrder(Guid id, Guid userId)
        {
            Entity entity = !(id == Guid.Empty) ? new WebProxyClient().Retrieve("illumina_purchaseorder", id, new ColumnSet(new string[6]
            {
        "illumina_name",
        "illumina_purchaseorderstatus",
        "illumina_supplierid",
        "illumina_fundapplicationid",
        "illumina_xerotrackingcodeid",
        "illumina_xeroid"
            })) : throw new Exception("Invalid id.");
            this.Id = entity != null ? entity.Id : throw new Exception("Invalid purchase order returned.");
            this.InitiatingUserId = userId;
            PurchaseOrder.FailureDescription = new List<string>();
            PurchaseOrder.Success = false;
            if (entity.Attributes.ContainsKey("illumina_name"))
                this.Name = (string)entity.Attributes["illumina_name"];
            if (entity.Attributes.ContainsKey("illumina_purchaseorderstatus"))
                this.PurchaseOrderStatus = (OptionSetValue)entity.Attributes["illumina_purchaseorderstatus"];
            if (entity.Attributes.ContainsKey("illumina_supplierid"))
                this.Supplier = (EntityReference)entity.Attributes["illumina_supplierid"];
            if (entity.Attributes.ContainsKey("illumina_fundapplicationid"))
                this.FundApplication = (EntityReference)entity.Attributes["illumina_fundapplicationid"];
            if (entity.Attributes.ContainsKey("illumina_xerotrackingcodeid"))
                this.XeroTrackingCode = (EntityReference)entity.Attributes["illumina_xerotrackingcodeid"];
            if (!entity.Attributes.ContainsKey("illumina_xeroid"))
                return;
            this.XeroId = (string)entity.Attributes["illumina_xeroid"];
        }

   

        private static void SendFailureEmail(PurchaseOrder po, TraceWriter log)
        {
            Entity entity1 = new Entity("email");
            Entity entity2 = new Entity("activityparty");
            Entity entity3 = new Entity("activityparty");
            WebProxyClient webProxyClient = new WebProxyClient();
            entity2["partyid"] = (object)new EntityReference("queue", new Guid(Configuration.SenderQueueId));
            entity3["partyid"] = (object)new EntityReference("systemuser", po.InitiatingUserId);
            entity1["from"] = (object)new Entity[1] { entity2 };
            entity1["to"] = (object)new Entity[1] { entity3 };
            entity1["subject"] = (object)("[SEND FAILURE] " + po.Name);
            entity1["regardingobjectid"] = (object)new EntityReference(po.FundApplication.LogicalName, po.FundApplication.Id);
            entity1["directioncode"] = (object)true;
            entity1["description"] = (object)("Failed to send the PO due to the following: <ul>" + string.Join("", PurchaseOrder.FailureDescription.ToArray()) + "</ul>");
            entity1.Id = webProxyClient.Create(entity1);
            SendEmailRequest request = new SendEmailRequest()
            {
                EmailId = entity1.Id,
                IssueSend = true
            };
            if (webProxyClient.Execute((OrganizationRequest)request) is SendEmailResponse)
                log.Info(string.Format("Purchase Order({0}): Failure Email({1}): Sent", (object)po.Id, (object)entity1.Id));
            else
                log.Info(string.Format("Purchase Order({0}): Failure Email({1}): Failed sending", (object)po.Id, (object)entity1.Id));
        }

        private static void UpdatePOStatus(PurchaseOrder po, OptionSetValue optionSetValue)
        {
            Entity entity = new Entity("illumina_purchaseorder", po.Id);
            entity["illumina_purchaseorderstatus"] = (object)optionSetValue;
            WebProxyClient webProxyClient = new WebProxyClient();
            webProxyClient.CallerId = po.InitiatingUserId;
            webProxyClient.Update(entity);
        }

        public static void Test_ClearXeroId(string id, string value) => new WebProxyClient().Update(new Entity("account", new Guid(id))
        {
            ["illumina_xeroid"] = string.IsNullOrEmpty(value) ? (object)null : (object)value
        });

        private static bool SendEmail(PurchaseOrder po, byte[] pdf, TraceWriter log)
        {
            try
            {
                Entity entity1 = new Entity("email");
                Entity entity2 = new Entity("activityparty");
                Entity entity3 = new Entity("activityparty");
                WebProxyClient orgSvc = new WebProxyClient();
                Entity supplier = orgSvc.Retrieve(po.Supplier.LogicalName, po.Supplier.Id, new ColumnSet(new string[4]
                {
          "emailaddress1",
          "preferredcontactmethodcode",
          "illumina_taxinvoicerequired",
          "fax"
                }));
                Entity entity4 = orgSvc.Retrieve(po.FundApplication.LogicalName, po.FundApplication.Id, new ColumnSet(new string[1]
                {
          "illumina_contact"
                }));
                OptionSetValue attributeValue1 = supplier.GetAttributeValue<OptionSetValue>("preferredcontactmethodcode");
                log.Info(string.Format("Purchase Order({0}): Supplier retrieved", (object)po.Id));
                entity2["partyid"] = (object)new EntityReference("queue", new Guid(Configuration.SenderQueueId));
                if (Convert.ToBoolean(attributeValue1.Value))
                {
                    log.Info(string.Format("Purchase Order({0}): Email is actually an email", (object)po.Id));
                    entity3["partyid"] = (object)new EntityReference(supplier.LogicalName, supplier.Id);
                }
                else
                {
                    log.Info(string.Format("Purchase Order({0}): Email is actually a fax", (object)po.Id));
                    string attributeValue2 = supplier.GetAttributeValue<string>("fax");
                    if (attributeValue2 == null)
                    {
                        PurchaseOrder.FailureDescription.Add("<li>Fax number field is empty.</li>");
                        throw new InvalidPluginExecutionException("Fax number field is empty");
                    }
                    string str = Regex.Replace(attributeValue2, "\\s+", "");
                    entity3["addressused"] = (object)(str + "@send.gofax.com.au");
                }
                log.Info(string.Format("Purchase Order({0}): Parties set", (object)po.Id));
                Entity entity5 = PurchaseOrder.InstantiateEmailFromTemplate(po, orgSvc, supplier);
                log.Info(string.Format("Purchase Order({0}): Email instantiated from template", (object)po.Id));
                entity5["from"] = (object)new Entity[1] { entity2 };
                entity5["to"] = (object)new Entity[1] { entity3 };
                entity5["subject"] = (object)(po.Name + " - " + entity4.GetAttributeValue<EntityReference>("illumina_contact").Name);
                entity5["regardingobjectid"] = (object)new EntityReference(po.FundApplication.LogicalName, po.FundApplication.Id);
                entity5["directioncode"] = (object)true;
                log.Info(string.Format("Purchase Order({0}): Email info set", (object)po.Id));
                entity5.Id = orgSvc.Create(entity5);
                log.Info(string.Format("Purchase Order({0}): Email({1}): Created", (object)po.Id, (object)entity5.Id));
                Entity entity6 = new Entity("activitymimeattachment");
                entity6["subject"] = entity5["subject"];
                entity6["filename"] = (object)(entity5["subject"]?.ToString() + ".pdf");
                entity6["body"] = (object)Convert.ToBase64String(pdf);
                entity6["objectid"] = (object)entity5.ToEntityReference();
                entity6["objecttypecode"] = (object)"email";
                log.Info(string.Format("Purchase Order({0}): Attachment info set", (object)po.Id));
                entity6.Id = orgSvc.Create(entity6);
                log.Info(string.Format("Purchase Order({0}): Attachment({1}): Created", (object)po.Id, (object)entity6.Id));
                SendEmailRequest request = new SendEmailRequest()
                {
                    EmailId = entity5.Id,
                    IssueSend = true
                };
                if (orgSvc.Execute((OrganizationRequest)request) is SendEmailResponse)
                {
                    log.Info(string.Format("Purchase Order({0}): Email({1}): Sent", (object)po.Id, (object)entity5.Id));
                    return true;
                }
                log.Info(string.Format("Purchase Order({0}): Email({1}): Failed sending", (object)po.Id, (object)entity5.Id));
                return false;
            }
            catch (Exception ex)
            {
                PurchaseOrder.FailureDescription.Add("<li>Error occured when sending the email.</li>");
                throw new Exception(ex.ToString());
            }
        }

        private static Entity InstantiateEmailFromTemplate(
          PurchaseOrder po,
          WebProxyClient orgSvc,
          Entity supplier)
        {
            InstantiateTemplateRequest request = new InstantiateTemplateRequest()
            {
                ObjectId = po.Id,
                ObjectType = "illumina_purchaseorder",
                TemplateId = supplier.GetAttributeValue<bool>("illumina_taxinvoicerequired") ? new Guid(Configuration.RequiredTemplateId) : new Guid(Configuration.NotRequiredTemplateId)
            };
            return (orgSvc.Execute((OrganizationRequest)request) as InstantiateTemplateResponse).EntityCollection.Entities.FirstOrDefault<Entity>();
        }

        public static JObject HttpClientRequests(string reqType, string query, string payloadJson)
        {

            WebProxyClient webProxyClient = new WebProxyClient();
            string accessToken = (string)webProxyClient.HeaderToken;
            string endpoint = Configuration.D365ODataUrl;

            var httpclient = new HttpClient();
            httpclient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            httpclient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            httpclient.DefaultRequestHeaders.Add("OData-Version", "4.0");
            httpclient.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"*\"");
            httpclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpRequestMessage request = null;
            var response = new HttpResponseMessage();
            string responseContent = "";
            JObject responseJson = null;

            if (reqType.ToLower() == "get")
            {
                request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}{query}");
                response = httpclient.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    responseContent = response.Content.ReadAsStringAsync().Result;
                    try
                    {
                        responseJson = JObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {

                        new InvalidPluginExecutionException($"FinanceIntegration Azure Function Error: HttpRequest Type Post Failed - Could not parse the response."); ;
                    };
                }
                else
                {
                    throw new InvalidPluginExecutionException($"FinanceIntegration Azure Function Error: HttpRequest Type Get Failed.");
                }
            }


            if (reqType.ToLower() == "post")
            {
                request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}{query}");
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                response = httpclient.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    responseContent = response.Content.ReadAsStringAsync().Result;
                    try
                    {
                        responseJson = JObject.Parse(responseContent);
                    }
                    catch (Exception)
                    {

                        new InvalidPluginExecutionException($"FinanceIntegration Azure Function Error: HttpRequest Type Post Failed - Could not parse the response."); ;
                    }
                }
                else
                {
                    throw new InvalidPluginExecutionException($"FinanceIntegration Azure Function Error: HttpRequest Type Post Failed.");
                }
            }

            return responseJson;
        }



        public static void Process(BrokeredMessage poMessage, TraceWriter log)
            {
            PurchaseOrder po = null;
            byte[] pdf = null;
            string poId = null;

            try
                {
                log.Info("enter process PO --------------->");

                var executionContext = poMessage.GetBody<RemoteExecutionContext>();

                if (executionContext.InputParameters.Contains("Target") &&
                      executionContext.InputParameters["Target"] is Entity target &&
                         target.LogicalName == "illumina_purchaseorder")
                    {
                    if (target.Contains("illumina_purchaseorderid"))
                        {
                        poId = target["illumina_purchaseorderid"].ToString();
                        }
                    foreach (var attribute in target.Attributes)
                        {
                        log.Info($"Attribute: {attribute.Key} = {attribute.Value}");
                        }
                    }
                Guid poCRMId = ConvertToGuid(poId);
                log.Info($"poCRMId ---- {poCRMId}");


                Entity poDocx = GetNoteByAnnotationId(poCRMId,log);
                log.Info($"poDocx ---- {poDocx != null}");
                if (poDocx == null)
                    throw new InvalidPluginExecutionException($"Purchase Order({poCRMId}): Could not find PO document.");

               
                // Step 2: Convert the docx to PDF - Now po.Name should be available
                pdf = ConvertToPDF(poDocx, po.Name, log);

                if (pdf == null || pdf.Length == 0)
                    throw new InvalidPluginExecutionException($"Purchase Order({poCRMId}): Could not convert PO document.");

                // Step 3: Send the Email
                if (SendEmail(po, pdf, log))
                    {
                    log.Info($"Purchase Order({po.Id}): Sent successfully.");

                    UpdatePOStatus(po, new OptionSetValue(390950002));  // Sent to Supplier
                    Success = true;
                    }
                else
                    {
                    log.Info($"Purchase Order({po.Id}): Sending failed.");

                    UpdatePOStatus(po, new OptionSetValue(390950005));  // Send to Supplier Failed
                    }
                }
            catch (Exception ex)
                {
                if (po != null && !Success)
                    {
                    SendFailureEmail(po, log);
                    }

                if (po != null)
                    {
                    UpdatePOStatus(po, new OptionSetValue(390950005));  // Send to Supplier Failed
                    }

                log.Error($"Error processing Purchase Order", ex);
                }
            }



        private static Entity GetNoteByAnnotationId(Guid id,TraceWriter log)
            {
            try
                {
                WebProxyClient orgService = new WebProxyClient();
                DataCollection<Entity> notes = orgService.RetrieveMultiple(new QueryExpression("annotation")
                    {
                    ColumnSet = new ColumnSet("isdocument", "filename", "documentbody", "annotationid", "createdon"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                        {
                        Conditions = {
						new ConditionExpression("objectid", ConditionOperator.Equal, id),
                        new ConditionExpression("isdocument", ConditionOperator.Equal, true)	// Documents only
					}
                        },
                    Orders = {
                    new OrderExpression("createdon", OrderType.Descending)
                }
                    }).Entities;
                if (notes.Count > 0)
                    return notes.FirstOrDefault();
                else
                    {
                    log.Info($"<li>Could not get the word template from the PO record in CRM.</li>");

                    return null;
                    }
                }
            catch (Exception e)
                {
                log.Error($"<li>Could not get the word template from the PO record in CRM.</li>");

                throw new Exception(e.ToString());
                }
            }


        private static byte[] ConvertToPDF(Entity poDocx, string name, TraceWriter log)
            {
            using (HttpClient client = new HttpClient())
                {
                ConverterPayload converterPayload = new ConverterPayload
                    {
                    FileName = $"{name}.docx",
                    Content = poDocx.GetAttributeValue<string>("documentbody"),
                    };

                HttpResponseMessage resp = client.PostAsJsonAsync(Environment.GetEnvironmentVariable("pdfConverterUri"), converterPayload).Result;

                if (resp.StatusCode == HttpStatusCode.OK)
                    {
                    return resp.Content.ReadAsByteArrayAsync().Result;
                    }
                else
                    {

                    FailureDescription.Add($"<li>Could not convert Word Template to PDF.</li>");
                    return Array.Empty<byte>();
                    }
                }
            }


        public class PdfPayload
        {
            [JsonProperty("EntityTypeCode")]
            public int EntityTypeCode { get; set; }

            [JsonProperty("SelectedTemplate")]
            public PdfTemplatePayload SelectedTemplate { get; set; }

            [JsonProperty("SelectedRecords")]
            public string SelectedRecords { get; set; }
        }

        public class PdfTemplatePayload
        {
            [JsonProperty("@odata.type")]
            public string ODataType { get; set; }

            [JsonProperty("documenttemplateid")]
            public string documenttemplateid { get; set; }
        }


        private static Guid ConvertToGuid(string input)
            {
            if (string.IsNullOrEmpty(input))
                {
                throw new ArgumentException("Project ID cannot be null or empty");
                }
            if (Guid.TryParse(input, out Guid result))
                {
                return result;
                }
            string cleaned = input.Trim().Replace("{", "").Replace("}", "").Replace("-", "");
            if (Guid.TryParse(cleaned, out result))
                {
                return result;
                }

            throw new ArgumentException($"Invalid GUID format: {input}");
            }


        public enum StatusCodes
        {
            AwaitingDelivery = 390950001, // 0x174D6C71
            SentToSupplier = 390950002, // 0x174D6C72
            SentToXeroUpdated = 390950006, // 0x174D6C76
        }
    }
}
