// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.PurchaseOrderGeneration
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model.Dynamics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;

namespace FinanceIntegration
{
    public static class PurchaseOrderGeneration
    {

        [FunctionName("PurchaseOrderGeneration")]
        public static void  Run([ServiceBusTrigger("purchaseorder", AccessRights.Listen, Connection = "SBConnectionString")]
             BrokeredMessage brokeredMessage,
          TraceWriter log)
            {
                log.Info("PurchaseOrderGeneration: Begin processing message......");
                if (brokeredMessage != null)
                {
                PurchaseOrder.Process(brokeredMessage, log);
                }
                else
                    log.Error("PurchaseOrderGeneration: Empty Message.");
                log.Info("PurchaseOrderGeneration queue trigger function processed message: " + brokeredMessage);
            }
    }
}
