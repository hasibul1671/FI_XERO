using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using FinanceIntegration.Model;
using System.Collections.Generic;
using System.Linq;
using Xero.Api.Core.Model;
using System.Web.Http.Results;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Xero.Api.Infrastructure.ThirdParty.ServiceStack.Text;
using System.Web.UI;
using FinanceIntegration.Model.Xero;
using Xero.Api.Core.Model.Setup;
using FinanceIntegration.Model.Dynamics;
using Microsoft.Xrm.Sdk.Query;
using System.Security.Cryptography;
using System.Text.Json;


namespace FinanceIntegration
    {
    public static class TimeEntrySyncTrigger
        {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string CREATE_JOB_URL = "https://api.xero.com/projects.xro/2.0/Projects/{0}/Tasks";
        private const string UPDATE_JOB_URL = "https://api.xero.com/projects.xro/2.0/Projects/{0}/Tasks/{0}";
        private const string TENANT_ID = "123da652-4c85-49fe-8128-c056bad09ada";
        private const string CONTACT_ID = "960a7b40-1e7f-4362-a3b1-ede3bee7e3f5";
        private static string TOKEN_URL = "https://identity.xero.com/connect/token";






        [FunctionName("SyncTimeEntry")]
        public static async Task Run(
            [ServiceBusTrigger("timeentryqueue", AccessRights.Listen, Connection = "SBConnectionString")]
             BrokeredMessage brokeredMessage,
            TraceWriter log)
            {
            log.Info($"Waiting for 20 seconds before processing...");
            await Task.Delay(30000);

            string timeEntryDescription = null;
            string jobId = null;
            string timeEntryId = null;

            try
                {
                var executionContext = brokeredMessage.GetBody<RemoteExecutionContext>();

                if (executionContext.InputParameters.Contains("Target") &&
                      executionContext.InputParameters["Target"] is Entity target &&
                         target.LogicalName == "illumina_timeentry")
                    {
                    if (target.Contains("subject"))
                        {
                        timeEntryDescription = target["subject"].ToString();
                        timeEntryId = target["activityid"].ToString();
                        }
                    foreach (var attribute in target.Attributes)
                        {
                        log.Info($"Attribute: {attribute.Key} = {attribute.Value}");
                        }
                    }
                //get crm jobid by this get synced crmxeroid 
                log.Info($"----timeEntryId {timeEntryId} ");
                jobId = GetCRMJobId(timeEntryId);

                var projectId = GetCRMProjectId(timeEntryId,log);

                log.Info($"----jobId {jobId} ----- projectId {projectId} ");

                var xeroProjectId = GetXeroProjectId(projectId);

                string accessToken = "";
                string CLIENT_ID = Environment.GetEnvironmentVariable("CLIENT_ID_XERO");
                string CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET_XERO");
                string REFRESH_TOKEN = "zr28JQzB8aZTTxqpzjL3v1tMsAPKlpkWyX_zdbAy9t8";
                var retrievedAccessToken = await GetAccessTokenFromRefreshToken(CLIENT_ID, CLIENT_SECRET, REFRESH_TOKEN, log);
                if (retrievedAccessToken == null)
                    {
                    log.Info("The Access Token was not retrieved properly.");
                    }
                else
                    {
                    accessToken = retrievedAccessToken;
                    }

                //get  synced crmxeroid for job
                var XeroJobId = RetrieveXeroJobID(jobId);
                //get userid from xero


                //sync time entry
                await SyncXeroJob(accessToken, XeroJobId, timeEntryId, timeEntryDescription, log, xeroProjectId);
                }
                  catch (Exception ex)
                {
                log.Error($"error--------------------> : {ex.Message}", ex);
                }
            }

        public static async Task<string> GetAccessTokenFromRefreshToken(string ClientID, string ClientSecret, string RefreshToken, TraceWriter log)
            {
            AzureTableConfiguration table = new AzureTableConfiguration();
            var firstRecord = table.FirstOrDefault();
            RefreshToken = firstRecord.RefreshToken;
            var body = new StringContent(
              $"grant_type=refresh_token&refresh_token={RefreshToken}&client_id={ClientID}&client_secret={ClientSecret}",
              Encoding.UTF8,
              "application/x-www-form-urlencoded");

            var response = await httpClient.PostAsync(TOKEN_URL, body);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                {
                log.Error($"Failed to refresh access token. Status: {response.StatusCode}, Response: {content}");
                throw new Exception("Token refresh failed.");
                }

            dynamic tokenResponse = JsonConvert.DeserializeObject(content);
            string newAccessToken = tokenResponse.access_token;

            string newRefreshToken = tokenResponse.refresh_token;
            firstRecord.RefreshToken = newRefreshToken;
            table.Update();
            log.Info("Successfully refreshed access token.");
            return newAccessToken;
            }




        public static async Task SyncXeroJob(string accessToken, string xeroJobId, string timeEntryId, string timeEntryDescription, TraceWriter log, string xeroProjectId)
            {

            string userId = await GetUserIdFromXero(accessToken, log);
            var (xeroTimeEntryId, isTimeEntrySync) = HandleIsTimeEntrySync(timeEntryId, log);
            if (isTimeEntrySync)
                {
                await UpdateXeroProject(userId, accessToken, timeEntryDescription, xeroTimeEntryId, xeroJobId, log, xeroProjectId);
                }
            else
                {
                _ = CreateXeroTaskAsync(userId, accessToken, timeEntryDescription, timeEntryId, xeroJobId, log, xeroProjectId);
                }
            }



        public static async Task UpdateXeroProject(string userId, string accessToken, string timeEntryDescription, string xeroTimeEntryId, string xeroJobId, TraceWriter log,
           string xeroProjectId)
            {

            var jobData = new
                {
                userId = userId,
                taskId = xeroJobId,
                dateUtc = "2025-01-01T23:34:15Z",
                duration = 120,
                description = timeEntryDescription
                };

            var content = new StringContent(
                JsonConvert.SerializeObject(jobData),
                Encoding.UTF8,
                "application/json");

            var updateUrl = $"https://api.xero.com/projects.xro/2.0/Projects/{xeroProjectId}/Time/{xeroTimeEntryId}";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, string.Format(updateUrl, xeroProjectId)))
                {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Headers.Add("Xero-Tenant-Id", TENANT_ID);
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    {
                    log.Info($"JOB updated successfully! Response: {responseContent}");
                    }
                else
                    {
                    log.Error($"Failed to update JOB. Status: {response.StatusCode}, Response: {responseContent}");
                    throw new Exception($"Failed to update Xero project: {response.StatusCode}");
                    }
                }
            }

        public static async Task CreateXeroTaskAsync(string userId, string accessToken, string timeEntryDescription, string timeEntryId, string xeroJobId, TraceWriter log, string xeroProjectId)
            {
            var jobData = new
                {
                userId = userId,
                taskId = xeroJobId,
                dateUtc = "2025-01-01T23:34:15Z",
                duration = 120,
                description = timeEntryDescription
                };
            var content = new StringContent(
                JsonConvert.SerializeObject(jobData),
                Encoding.UTF8,
                "application/json");
            var jobCreateUrl = $"https://api.xero.com/projects.xro/2.0/Projects/{xeroProjectId}/Time";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, jobCreateUrl))
                {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Headers.Add("Xero-Tenant-Id", TENANT_ID);
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();
                log.Info($"responseContent: {responseContent}");

                if (response.IsSuccessStatusCode)
                    {
                    try
                        {
                        dynamic createdProject = JsonConvert.DeserializeObject(responseContent);
                        if (createdProject != null && createdProject.taskId != null)
                            {
                            string xeroTimeEntryId = createdProject.timeEntryId.ToString();
                            SaveXeroTimeEntryIdToCrm(xeroTimeEntryId, timeEntryId, log);

                            }
                        }
                    catch (Exception ex)
                        {
                        log.Error($"Error extracting or saving Xero project ID: {ex.Message}", ex);
                        }
                    }
                else
                    {
                    log.Error($"Failed to create project in XERO. Status: {response.StatusCode}, Response: {responseContent}");
                    throw new Exception($"Failed to create Xero project: {response.StatusCode}");
                    }
                }
            }






        public static async Task<string> GetUserIdFromXero(string accessToken, TraceWriter log)
            {
            string userId = null;
            var url = "https://api.xero.com/projects.xro/2.0/projectsusers";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, url))
                {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Headers.Add("Xero-Tenant-Id", TENANT_ID);
                var response = await httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                    {
                    log.Info($"Get User successfully! Response: {responseContent}");

                    // Use Newtonsoft.Json instead of System.Text.Json
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
                    userId = jsonResponse.items[0].userId;
                    }
                }
            return userId;
            }






        public static string GetCRMJobId(string jobId)
            {
            Guid crmJobId = ConvertToGuid(jobId);
            Entity projectEntity = new WebProxyClient().Retrieve("illumina_timeentry", crmJobId, new ColumnSet("regardingobjectid"));

            var projectReference = projectEntity.GetAttributeValue<EntityReference>("regardingobjectid");
            return projectReference?.Id.ToString();
            }

        public static string GetCRMProjectId(string timeEntryId, TraceWriter log)
            {
            try
                {
                Guid crmTimeEntryId = ConvertToGuid(timeEntryId);
                log.Info($"Attempting to retrieve time entry with ID: {crmTimeEntryId}");

                Entity timeEntryEntity = new WebProxyClient().Retrieve("illumina_timeentry", crmTimeEntryId, new ColumnSet("illumina_project"));

                if (timeEntryEntity == null)
                    {
                    log.Error($"Time entry with ID {crmTimeEntryId} not found");
                    return null;
                    }

                var projectReference = timeEntryEntity.GetAttributeValue<EntityReference>("illumina_project");

                if (projectReference == null)
                    {
                    log.Warning($"Project reference is null for time entry ID {crmTimeEntryId}. The project field may not be populated yet.");
                    return null;
                    }

                log.Info($"Successfully retrieved project reference with ID: {projectReference.Id}");
                return projectReference.Id.ToString();
                }
            catch (Exception ex)
                {
                log.Error($"Error in GetCRMProjectId: {ex.Message}");
                return null;
                }
            }




        public static string GetXeroProjectId(string projectId)
            {
            string xeroProjectId = null;
            Guid crmProjectId = ConvertToGuid(projectId);
            Entity projectEntity = new WebProxyClient().Retrieve("illumina_project", crmProjectId, new ColumnSet("crd27_xeroprojectid"));
            xeroProjectId = projectEntity.GetAttributeValue<string>("crd27_xeroprojectid");
            return xeroProjectId;

            }





        public static string RetrieveXeroJobID(string jobId)
            {
            Guid crmJobId = ConvertToGuid(jobId);
            Entity projectEntity = new WebProxyClient().Retrieve("illumina_projecttask", crmJobId, new ColumnSet("crd27_xerojobid"));
            var xeroJobId = projectEntity.GetAttributeValue<string>("crd27_xerojobid");
            return xeroJobId;
            }



        public static (string xeroTimeEntryId, bool isTimeEntrySync) HandleIsTimeEntrySync(string timeEntryId, TraceWriter log)
            {
            string xeroTimeEntryId = null;
            bool isTimeEntrySync = false;

            try
                {
                Guid crmTimeEntryId = ConvertToGuid(timeEntryId);
                Entity xEntity = new WebProxyClient().Retrieve("illumina_timeentry", crmTimeEntryId, new ColumnSet("crd27_xerotimeentryid"));
                if (xEntity != null && xEntity.Contains("crd27_xerotimeentryid"))
                    {
                log.Info("time entry is aleready sunceddd_----------------------------------->");
                    xeroTimeEntryId = xEntity.GetAttributeValue<string>("crd27_xerotimeentryid");
                    isTimeEntrySync = !string.IsNullOrEmpty(xeroTimeEntryId);
                    }
                else
                    {
                    log.Info("Xero timeEntry ID not found. timeEntry is not synced.");
                    }
                }
            catch (Exception ex)
                {
                log.Error($"Error retrieving timeEntry from CRM: {ex.Message}", ex);
                }

            return (xeroTimeEntryId, isTimeEntrySync);
            }




        public static void SaveXeroTimeEntryIdToCrm(
        string xeroTimeEntryId,
        string timeEntryId,
        TraceWriter log)
            {
            log.Info($"Enter SaveXeroTimeEntryIdToCrm  Method--------------> {xeroTimeEntryId}----{timeEntryId}");
            try
                {
                Guid CrmTimeEntryId = ConvertToGuid(timeEntryId);
                new WebProxyClient().Update(new Entity("illumina_timeentry", CrmTimeEntryId)
                    {
                    ["crd27_xerotimeentryid"] = (object)xeroTimeEntryId
                    });
                log.Info("Exiting - SaveXeroTimeEntryIdToCrm");

                }
            catch (Exception ex)
                {
                log.Error($"Error updating CRM Project Exception: {ex.Message}", ex);

                if (ex is AggregateException aggEx)
                    {
                    foreach (var inner in aggEx.InnerExceptions)
                        {
                        log.Error($"Inner exception: {inner.Message}", inner);
                        }
                    }
                log.Error($"Base exception: {ex.GetBaseException().Message}", ex.GetBaseException());
                }

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


        }
    }






