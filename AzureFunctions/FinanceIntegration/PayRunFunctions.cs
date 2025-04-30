
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web.Http.Results;
using System.Web;

namespace FinanceIntegration
    {
    public class PayRunFunctions
        {
        private readonly ILogger<PayRunFunctions> _logger;
        private readonly ATMSModel.Configuration _configurations;
        private readonly FinanceIntegration.Services.PayRunService _payrunService;

        public PayRunFunctions(ATMSModel.Configuration configurations, FinanceIntegration.Services.PayRunService payrunService,
                               ILogger<PayRunFunctions> logger)
            {
            _configurations = configurations;
            _logger = logger;
            _payrunService = payrunService;
            }

        // http trigger event to push payrun into xero
        [FunctionName("PayRuyDraftPushHttpEvent")]
        public async Task<IActionResult> PayRuyDraftPushHttpEvent([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
            {
            PayRunRequest request = await JsonSerializer.DeserializeAsync<PayRunRequest>(req.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            log?.LogInformation($"PayRunHttpEvent: Processing Id {request.PayRunRequestId}, for {request.Organisation}.");

            try
                {
                _payrunService.PushDaftPayRun(log, request.Organisation, request.PayRunRequestId?.ToString());
                }
            catch (Exception e)
                {
                log?.LogError($"TimesheetHttp: Error processing {request.PayRunRequestId}), for {request.Organisation}: {e.Message}");
                return new UnprocessableEntityObjectResult(e.Message);
                }

            log?.LogInformation($"TimesheetHttp: Finished processing ({request.PayRunRequestId}), for {request.Organisation}.");
            return new OkResult();
            }

        //
        // timer event to process payrun
        //
        [FunctionName("PayRunTimerEventSync")]
        public async Task PayRunTimerEventSync([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
            {
            //var log = context.GetLogger<PayRunFunctions>();
            log?.LogInformation($"PayRunTimerEvent: Processing payrun at {DateTime.Now}");

            try
                {
                _payrunService.SyncPayRuns(log);
                }
            catch (Exception e)
                {
                log?.LogError($"PayRunTimerEvent: Error processing payrun: {e.Message}");
                }

            log?.LogInformation($"PayRunTimerEvent: Finished processing payrun at {DateTime.Now}");
            }


        // http trigger event to manualy run timer event
        [FunctionName("PayRunHttpEventSync")]
        public async Task<IActionResult> PayRunHttpEventSync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
            {
            log?.LogInformation($"PayRunHttpEvent: Manual trigger at {DateTime.Now}");
            try
                {
                _payrunService.SyncPayRuns(log);
                }
            catch (Exception e)
                {
                log?.LogError($"PayRunHttpEvent: Error processing payrun: {e.Message}");
                return new UnprocessableEntityObjectResult(e.Message);
                }

            log?.LogInformation($"PayRunHttpEvent: Finished processing payrun at {DateTime.Now}");
            return new OkResult();
            }

        public class PayRunRequest
            {
            public string Organisation { get; set; } = null;
            public Guid? PayRunRequestId { get; set; } = null; // CRM payrunrequestid 
            }
        }
    }
