// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.XeroTrigger
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model;
using FinanceIntegration.Model.Dynamics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FinanceIntegration
{
    public static class XeroTrigger
    {
        [FunctionName("XeroTrigger")]
        public static void Run([ServiceBusTrigger("xero", AccessRights.Listen, Connection = "SBConnectionString")] string myQueueItem, TraceWriter log)
        {
            log.Info("XeroTrigger: Begin processing message.");
            JObject jObject = JsonConvert.DeserializeObject<JObject>(myQueueItem);
            jObject.FixEntityReference("illumina_supplierid");
            jObject.FixEntityReference("illumina_fundapplicationid");
            jObject.FixEntityReference("illumina_xerotrackingcodeid");
            PurchaseOrder purchaseOrder = jObject.ToObject<PurchaseOrder>();
            if (myQueueItem != null)
                XeroJob.Process(purchaseOrder, log);
            else
                log.Error("XeroTrigger: Empty Message.");
            log.Info("XeroTrigger queue trigger function processed message: " + myQueueItem);
        }

#if DEBUG
        // http trigger for testing
        [FunctionName("XeroTriggerHttp")]
        public static void RunHttp([HttpTrigger] string myQueueItem, TraceWriter log)
        {
            log.Info("XeroTrigger: Begin processing message.");
            JObject jObject = JsonConvert.DeserializeObject<JObject>(myQueueItem);
            jObject.FixEntityReference("illumina_supplierid");
            jObject.FixEntityReference("illumina_fundapplicationid");
            jObject.FixEntityReference("illumina_xerotrackingcodeid");
            PurchaseOrder purchaseOrder = jObject.ToObject<PurchaseOrder>();
            if (myQueueItem != null)
                XeroJob.Process(purchaseOrder, log);
            else
                log.Error("XeroTrigger: Empty Message.");
            log.Info("XeroTrigger queue trigger function processed message: " + myQueueItem);
        }
#endif
    }
}
