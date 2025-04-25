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


namespace FinanceIntegration
{
    public static class ProjectSyncTrigger
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string PROJECT_URL = "https://api.xero.com/projects.xro/2.0/Projects";
        private const string PROJECT_URL_UPDATE = "https://api.xero.com/projects.xro/2.0/Projects/{0}"; // Placeholder for projectId
        private const string PROJECT_URL_GET = "https://api.xero.com/projects.xro/2.0/Projects/{0}"; // For getting a specific project
        private const string TENANT_ID = "123da652-4c85-49fe-8128-c056bad09ada";

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
                string tenantId = null;


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

                log.Info($"------------------------accesstoken>{accessToken}");

                //tenantId = Model.Xero.XeroClient.getOrganization(accessToken);
                if (tenantId == null)
                {
                    log.Info("XeroClient : Error getting organization ID needed to post payloads as per Xero Api guide");
                }
                log.Info($"------------------------tenantId>{tenantId}");

                var contactId = "960a7b40-1e7f-4362-a3b1-ede3bee7e3f5";
                //Project project = null;
                //await SyncXeroProject(project, tenantId, accessToken, contactId, projectName, projectId, log);

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
            log.Info($"tokenResponse----------------- {tokenResponse}");


            string newRefreshToken = tokenResponse.refresh_token;
            firstRecord.RefreshToken = newRefreshToken;
            table.Update();
            log.Info($"Successfully refreshed access token. {tokenResponse}");
            log.Info("Successfully refreshed access token.");
            return newAccessToken;
        }




        public static async Task SyncXeroProject( string tenantId, string accessToken, string contactId, string projectName, string projectId, TraceWriter log)
        {


            await CreateXeroProject( tenantId, accessToken, contactId, projectName, projectId, log);
            //string existingXeroProjectId = GetXeroProjectId(projectId, log);

            string existingXeroProjectId = "DD";

            if (!string.IsNullOrEmpty(existingXeroProjectId))
            {
                await UpdateXeroProject(accessToken, contactId, projectName, projectId, log);
            }
            else
            {
                await CreateXeroProject( tenantId, accessToken, contactId, projectName, projectId, log);
            }
        }



        public static async Task UpdateXeroProject(string accessToken, string contactId, string projectName, string projectId, TraceWriter log)
        {
            var project = new
            {
                contactId = contactId,
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

        public static async Task CreateXeroProject( string tenantId, string accessToken, string contactId, string projectName, string projectId, TraceWriter log)
        {
            var projectJson = new
            {
                contactId = contactId,
                name = projectName,
                deadlineUtc = DateTime.UtcNow.AddMonths(3).ToString("yyyy-MM-ddTHH:mm:ss"),
                estimateAmount = 15000.00
            };


            //XeroProject returnProjectResponse = XeroClient.postProject(tenantId, accessToken, projectJson);
            //log.Info($"Project created successfully! returnProjectResponse: {returnProjectResponse}");
            //UpdateCRMProject(project, returnProjectResponse, log);



        }



        //public static void UpdateCRMProject(
        //FinanceIntegration.Model.Dynamics.Project project,
        //FinanceIntegration.Model.Xero.XeroProject returnProjectResponse,
        //TraceWriter log)
        //{
        //    log.Info("Entered - updatePurchaseOrder - Filling in CRM with created info");

        //    new WebProxyClient().Update(new Entity("illumina_project", project.Id)
        //    {
        //        ["crd27_xeroprojectid"] = (object)new OptionSetValue(1000),
        //    });
        //    log.Info("Exiting - updatePurchaseOrder");
        //}

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
