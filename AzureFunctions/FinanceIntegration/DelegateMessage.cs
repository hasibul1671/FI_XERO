// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.DelegateMessage
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model;
using FinanceIntegration.Model.Dynamics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinanceIntegration
{
    public static class DelegateMessage
    {
        [FunctionName("DelegateMessage")]
        public static void Run([ServiceBusTrigger("financedelegator", AccessRights.Manage, Connection = "SBConnectionString")] BrokeredMessage myQueueItem, TraceWriter log)
        {
            log.Info("DelegateMessage: Begin processing message.");
            if (myQueueItem != null)
            {
                log.Info("DelegateMessage(" + myQueueItem.MessageId + "): Let's do it!");
                ServiceBusMessage serviceBusMessage = new ServiceBusMessage(myQueueItem);
                if (serviceBusMessage.PrimaryEntityName == "illumina_purchaseorder")
                {
                    if (((IEnumerable<string>)new string[1] { "Update" }).Contains<string>(serviceBusMessage.MessageName))
                    {
                        log.Info(string.Format("DelegateMessage({0}): Processing {1} for {2}({3}).", (object)myQueueItem.MessageId, (object)serviceBusMessage.MessageName, (object)serviceBusMessage.PrimaryEntityName, (object)serviceBusMessage.PrimaryEntityId));

                        PurchaseOrder purchaseOrder = new PurchaseOrder(serviceBusMessage.PrimaryEntityId, serviceBusMessage.InitiatingUserId);
                        log.Info("DelegateMessage(" + myQueueItem.MessageId + "): PO Status is " + Enum.GetName(typeof(PurchaseOrder.StatusCodes), (object)purchaseOrder.PurchaseOrderStatus.Value) + ".");
                        if (purchaseOrder.PurchaseOrderStatus.Value == 390950001) // Awaiting Delivery - 390950000
                        {
                            log.Info("DelegateMessage(" + myQueueItem.MessageId + "): Off to PO Sender!");
                            var payload = (object)JsonConvert.SerializeObject((object)purchaseOrder);
                            // Call function - "PurchaseOrderGeneration"
                            QueueClient.CreateFromConnectionString(Configuration.SbConnectionString, "purchaseorder").Send(new BrokeredMessage(payload));
                        }
                        else if (purchaseOrder.PurchaseOrderStatus.Value == 390950002 || // Sent to Supplier      - 390950002,
                                 purchaseOrder.PurchaseOrderStatus.Value == 390950006)   // Sent to Xero - Update - 390950006,
                        {
                            log.Info("DelegateMessage(" + myQueueItem.MessageId + "): Off to Xero Sender!");
                            var payload = (object)JsonConvert.SerializeObject((object)purchaseOrder);
                            // Call function - "XeroTrigger"
                            QueueClient.CreateFromConnectionString(Configuration.SbConnectionString, "xero").Send(new BrokeredMessage(payload));
                        }
                        else
                        {
                            log.Info("We're in some trouble here.");
                        }
                    }
                }
            }
            else
                log.Error("DelegateMessage: Empty Message.");
            log.Info(string.Format("DelegateMessage queue trigger function processed message: {0}", (object)myQueueItem));
        }
    }
}
