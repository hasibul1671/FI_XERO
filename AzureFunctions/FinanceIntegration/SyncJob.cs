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


namespace FinanceIntegration
    {
    public static class JobSyncTrigger
        {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string CREATE_JOB_URL = "https://api.xero.com/projects.xro/2.0/Projects/{0}/Tasks";
        private const string UPDATE_JOB_URL = "https://api.xero.com/projects.xro/2.0/Projects/{0}/Tasks/{0}";
        private const string TENANT_ID = "123da652-4c85-49fe-8128-c056bad09ada";
        private const string CONTACT_ID = "960a7b40-1e7f-4362-a3b1-ede3bee7e3f5";
        private static string TOKEN_URL = "https://identity.xero.com/connect/token";






        [FunctionName("SyncJob")]
        public static async Task Run(
            [ServiceBusTrigger("jobqueue", AccessRights.Listen, Connection = "SBConnectionString")]
             BrokeredMessage brokeredMessage,
            TraceWriter log)
            {

            string jobName = null;
            string projectId = null;
            string jobId = null;

            try
                {
                var executionContext = brokeredMessage.GetBody<RemoteExecutionContext>();

                if (executionContext.InputParameters.Contains("Target") &&
                      executionContext.InputParameters["Target"] is Entity target &&
                         target.LogicalName == "illumina_projecttask")
                    {
                    if (target.Contains("illumina_title"))
                        {
                        jobName = target["illumina_title"].ToString();
                        }
                    jobId = target.Id.ToString();
                    foreach (var attribute in target.Attributes)
                        {
                        log.Info($"Attribute: {attribute.Key} = {attribute.Value}");
                        }
                    }

                projectId = GetCRMProjectId(jobId);

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
                var XeroProjectId = RetrieveXeroProjectID(projectId);
                await SyncXeroJob(accessToken, XeroProjectId, jobId, jobName, log);
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




        public static async Task SyncXeroJob(string accessToken, string xeroProjectId, string jobId, string jobName, TraceWriter log)
            {
            var (xeroJobId, isProjectSync) = HandleIsJobSync(jobId, log);
            if (isProjectSync)
                {
                await UpdateXeroProject(accessToken, jobName, xeroProjectId, xeroJobId, log);
                }
            else
                {
                _ = CreateXeroTaskAsync(accessToken, jobName, jobId, xeroProjectId, log);
                }
            }



        public static async Task UpdateXeroProject(string accessToken, string jobName, string xeroProjectId,string xeroJobId, TraceWriter log)
            {

            var jobData = new
                {
                name = jobName,
                rate = new
                    {
                    currency = "AUD",
                    value = 99.99m
                    },
                chargeType = "TIME",
                estimateMinutes = 120
                };

            var content = new StringContent(
                JsonConvert.SerializeObject(jobData),
                Encoding.UTF8,
                "application/json");

            var updateUrl = $"https://api.xero.com/projects.xro/2.0/Projects/{xeroProjectId}/Tasks/{xeroJobId}";
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

        public static async Task CreateXeroTaskAsync(string accessToken, string jobName, string jobId, string xeroProjectId, TraceWriter log)
            {
            var jobData = new
                {
                name = jobName,
                rate = new
                    {
                    currency = "AUD",
                    value = 99.99m
                    },
                chargeType = "TIME",
                estimateMinutes = 120
                };
            var content = new StringContent(
                JsonConvert.SerializeObject(jobData),
                Encoding.UTF8,
                "application/json");
            var jobCreateUrl = $"https://api.xero.com/projects.xro/2.0/Projects/{xeroProjectId}/Tasks";
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
                            string xeroJobId = createdProject.taskId.ToString();
                            SaveXeroJobIdToCrm(xeroJobId, jobId, log);
                           
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


        public static string GetCRMProjectId(string jobId)
            {
            Guid crmJobId = ConvertToGuid(jobId);
            Entity projectEntity = new WebProxyClient().Retrieve("illumina_projecttask", crmJobId, new ColumnSet("illumina_project"));

            var projectReference = projectEntity.GetAttributeValue<EntityReference>("illumina_project");
            return projectReference?.Id.ToString();
            }



        public static string RetrieveXeroProjectID(string projectId)
            {
            Guid crmProjectId = ConvertToGuid(projectId);
            Entity projectEntity = new WebProxyClient().Retrieve("illumina_project", crmProjectId, new ColumnSet("crd27_xeroprojectid"));
            var xeroProjectId = projectEntity.GetAttributeValue<string>("crd27_xeroprojectid");
            return xeroProjectId;
            }

        public static (string xeroJobId, bool isJobSync) HandleIsJobSync(string jobId, TraceWriter log)
            {
            string xeroJobId = null;
            bool isJobSync = false;

            try
                {
                Guid crmProjectId = ConvertToGuid(jobId);
                Entity projectEntity = new WebProxyClient().Retrieve("illumina_projecttask", crmProjectId, new ColumnSet("crd27_xerojobid"));

                if (projectEntity != null && projectEntity.Contains("crd27_xerojobid"))
                    {
                    xeroJobId = projectEntity.GetAttributeValue<string>("crd27_xerojobid");
                    isJobSync = !string.IsNullOrEmpty(xeroJobId);
                    }
                else
                    {
                    log.Info("Xero Project ID not found. Project is not synced.");
                    }
                }
            catch (Exception ex)
                {
                log.Error($"Error retrieving project from CRM: {ex.Message}", ex);
                }

            return (xeroJobId, isJobSync);
            }




        public static void SaveXeroJobIdToCrm(
        string xeroProjectId,
        string jobId,
        TraceWriter log)
            {
            log.Info($"Enter SaveXeroProjectIdToCrm Method--------------> {xeroProjectId}----{jobId}");
            try
                {
                Guid CrmProjectId = ConvertToGuid(jobId);
                new WebProxyClient().Update(new Entity("illumina_projecttask", CrmProjectId)
                    {
                    ["crd27_xerojobid"] = (object)xeroProjectId
                    });
                log.Info("Exiting - SaveXeroIdToCrm");

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





   
