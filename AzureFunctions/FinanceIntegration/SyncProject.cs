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


namespace FinanceIntegration
{
    public static class ProjectSyncTrigger
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string PROJECT_URL = "https://api.xero.com/projects.xro/2.0/Projects";
        private const string PROJECT_URL_UPDATE = "https://api.xero.com/projects.xro/2.0/Projects/{0}"; // Placeholder for projectId
        private const string PROJECT_URL_GET = "https://api.xero.com/projects.xro/2.0/Projects/{0}"; // For getting a specific project
        private const string TENANT_ID = "123da652-4c85-49fe-8128-c056bad09ada";
        private const string CONTACT_ID= "960a7b40-1e7f-4362-a3b1-ede3bee7e3f5";

        private static string TOKEN_URL = "https://identity.xero.com/connect/token";


        [FunctionName("SyncProject")]
        public static async Task Run(
            [ServiceBusTrigger("projectqueue", AccessRights.Listen, Connection = "SBConnectionString")]
             BrokeredMessage brokeredMessage,
            TraceWriter log)
        {
            string projectName = "Default Project";
            string projectId = string.Empty;
            string xeroProjectId = "000111222";
            log.Info("XeroTrigger: Begin processing message.from P98---------------->");
                try
                {
                    var executionContext = brokeredMessage.GetBody<RemoteExecutionContext>();
                    log.Info($"Message details: Operation={executionContext.MessageName}, Entity={executionContext.PrimaryEntityName}");

                    if (executionContext.InputParameters.Contains("Target") &&
                        executionContext.InputParameters["Target"] is Entity target &&
                        target.LogicalName == "illumina_project")
                    {
                        log.Info($"Project ID from Dynamics: {target.Id}");
                        if (target.Contains("illumina_name"))
                        {
                            projectName = target["illumina_name"].ToString();
                            log.Info($"Project Name: {projectName}");

                            if (target.Contains("illumina_projectid"))
                            {
                                projectId = target["illumina_projectid"].ToString();
                                log.Info($"Project ID: {projectId}");
                            }

                        }
                        foreach (var attribute in target.Attributes)
                        {
                            log.Info($"Attribute: {attribute.Key} = {attribute.Value}");
                        }
                    }
                //Project project = new Project(target.Id.ToString(), executionContext.InitiatingUserId?.ToString());
                string accessToken = "";

                //string CLIENT_ID = Configuration.XeroClientId;
                //string CLIENT_SECRET = Configuration.XeroClientSecret;
                //string REFRESH_TOKEN = "dy7t4OQEse8l-Jhv3T3DJEW51DlG5esKgVdUEa17Sy4";


                string CLIENT_ID = Environment.GetEnvironmentVariable("CLIENT_ID_XERO");
                string CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET_XERO");
                string REFRESH_TOKEN = "zr28JQzB8aZTTxqpzjL3v1tMsAPKlpkWyX_zdbAy9t8";


                //string CLIENT_ID = "9E80E7F6C1AA4CA09050A5358BBE2347";
                //string CLIENT_SECRET = "UfpqpjhbZKLHKOhn_YDnZD9GDRSe8hAaPN2mr4hM_9jACAcg";
                //string REFRESH_TOKEN = "dy7t4OQEse8l-Jhv3T3DJEW51DlG5esKgVdUEa17Sy4";

                var retrievedAccessToken = await GetAccessTokenFromRefreshToken(CLIENT_ID,CLIENT_SECRET,REFRESH_TOKEN, log);
                if (retrievedAccessToken == null)
                {
                    log.Info("The Access Token was not retrieved properly.");
                }
                else
                {
                    accessToken = retrievedAccessToken;
                }

               
                
                //Project project = null;
                await SyncXeroProject(accessToken, projectName, projectId, log);

                //UpdateCRMProject(project, xeroProjectId, ConvertToGuid(projectId), log);

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




        public static async Task SyncXeroProject( string accessToken, string projectName, string projectId, TraceWriter log)
        {


            await CreateXeroProject(accessToken, projectName, projectId, log);
            //string existingXeroProjectId = GetXeroProjectId(projectId, log);

            //string existingXeroProjectId = "DD";

            //if (!string.IsNullOrEmpty(existingXeroProjectId))
            //{
            //    await UpdateXeroProject(accessToken, projectName, projectId, log);
            //}
            //else
            //{
            //    CreateXeroProject(accessToken, projectName, projectId, log);
            //}
        }



        public static async Task UpdateXeroProject(string accessToken, string projectName, string projectId, TraceWriter log)
        {
            var project = new
            {
                contactId = CONTACT_ID,
                name = projectName,
                estimateAmount = 15000.00,
                deadlineUtc = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ss")
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(project),
                Encoding.UTF8,
                "application/json");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, string.Format(PROJECT_URL_UPDATE, projectId)))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Headers.Add("Xero-Tenant-Id", TENANT_ID);
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    log.Info($"Project updated successfully! Response: {responseContent}");
                }
                else
                {
                    log.Error($"Failed to update project. Status: {response.StatusCode}, Response: {responseContent}");
                    throw new Exception($"Failed to update Xero project: {response.StatusCode}");
                }
            }
        }

        public static async Task CreateXeroProject(string accessToken, string projectName, string projectId, TraceWriter log)
        {
            var project = new
            {
                contactId = CONTACT_ID,
                name = projectName,
                deadlineUtc = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                estimateAmount = 15000.00
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(project),
                Encoding.UTF8,
                "application/json");

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, PROJECT_URL))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                requestMessage.Headers.Add("Xero-Tenant-Id", TENANT_ID);
                requestMessage.Content = content;

                var response = await httpClient.SendAsync(requestMessage);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        dynamic createdProject = JsonConvert.DeserializeObject(responseContent);
                        if (createdProject != null && createdProject.projectId != null)
                        {
                            string xeroProjectId = createdProject.projectId.ToString();
                             SaveXeroProjectIdToCrm(xeroProjectId, projectId,log);
                            log.Info($"Project created successfully in XERO Response: {xeroProjectId}");

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
        //illumina_xeroprojectid

        

        public static void SaveXeroProjectIdToCrm(
        string xeroProjectId,
        string projectId,
        TraceWriter log)
        {
            log.Info("Enter SAVEProjectID TO CRM - -------------->");
            try
            {
                Guid CrmProjectId = ConvertToGuid(projectId);
                log.Info("$Entered - Update Project in CRM GUID- {CrmProjectId}");
                new WebProxyClient().Update(new Entity("illumina_project", CrmProjectId)
                {
                    ["crd27_xeroprojectid"] = (object)xeroProjectId
                });
                log.Info("Exiting - SaveXeroProjectIdToCrm");

            }
            catch (Exception ex)
            {
                // Log the top-level message
                log.Error($"Error updating CRM Project Exception: {ex.Message}", ex);

                // Unwrap aggregate exceptions
                if (ex is AggregateException aggEx)
                {
                    foreach (var inner in aggEx.InnerExceptions)
                    {
                        log.Error($"Inner exception: {inner.Message}", inner);
                    }
                }

                // Also log the base exception
                log.Error($"Base exception: {ex.GetBaseException().Message}", ex.GetBaseException());
            }

        }






        private static Guid ConvertToGuid(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Project ID cannot be null or empty");
            }

            // Try to parse as-is
            if (Guid.TryParse(input, out Guid result))
            {
                return result;
            }

            // Try cleaning up the input (removing whitespace, braces, etc.)
            string cleaned = input.Trim().Replace("{", "").Replace("}", "").Replace("-", "");
            if (Guid.TryParse(cleaned, out result))
            {
                return result;
            }

            throw new ArgumentException($"Invalid GUID format: {input}");
        }


    }





    public class ProjectData : IProjectData
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
    }

    public interface IProjectData
    {
    }
}
