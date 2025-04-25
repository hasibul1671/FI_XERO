// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.XeroWebHook
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model;
using FinanceIntegration.Model.Xero;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace FinanceIntegration
{
    public static class XeroWebHook
    {
        [FunctionName("XeroWebHook")]
        public static void Run([ServiceBusTrigger("xerowebhook", AccessRights.Listen, Connection = "SBConnectionString")] string myQueueItem, TraceWriter log)
        {
            log.Info("XeroWebHook: Begin processing message.");
            XeroEvent newEvent = JsonConvert.DeserializeObject<XeroEvent>(myQueueItem);
            log.Info("XeroWebHook: JSON Parse Success - Passing to Proccess");
            TraceWriter log1 = log;
            XeroWebHookJob.Process(newEvent, log1);
            log.Info("XeroWebHook queue trigger function processed message: " + myQueueItem);
        }
    }
}
