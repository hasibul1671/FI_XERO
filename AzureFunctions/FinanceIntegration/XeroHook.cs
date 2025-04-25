// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.XeroHook
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model;
using FinanceIntegration.Model.Xero;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FinanceIntegration
{
    public static class XeroHook
    {
        private static Dictionary<KeyValuePair<XeroHookCategory, XeroHookEvent>, Action<XeroEvent>> xeroActions;

        [FunctionName("XeroHook")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, new string[] { "post" }, Route = null)] HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            log.Info("Configuration.XeroHookKey=" + Configuration.XeroHookKey);
            try
            {
                log.Info("Configuration.XeroHookKey=" + Configuration.XeroHookKey);
                IEnumerable<string> values;
                req.Headers.TryGetValues("x-xero-signature", out values);
                string cCode = values != null ? values.FirstOrDefault<string>() : (string)null;
                log.Info("cCode=" + cCode);
                string json = await req.Content.ReadAsStringAsync();
                log.Info("HashData=" + XeroHook.HashData(json, Configuration.XeroHookKey));
                if (cCode == null || !XeroHook.HashData(json, Configuration.XeroHookKey).SequenceEqual<char>((IEnumerable<char>)cCode))
                    return req.CreateResponse(HttpStatusCode.Unauthorized);
                XeroPublish data = JsonConvert.DeserializeObject<XeroPublish>(json);
                if (data.Events.Any<XeroEvent>())
                    Task.Factory.StartNew((Action)(() =>
                    {
                        foreach (XeroEvent xeroEvent in data.Events)
                        {
                            KeyValuePair<XeroHookCategory, XeroHookEvent> key = new KeyValuePair<XeroHookCategory, XeroHookEvent>(xeroEvent.EventCategory, xeroEvent.EventType);
                            if (XeroHook.XeroActions.ContainsKey(key))
                            {
                                log.Info("processing " + xeroEvent.EventCategory.ToString() + ", " + xeroEvent.EventType.ToString() + " for " + xeroEvent.ResourceId.ToString());
                                XeroHook.XeroActions[key](xeroEvent);
                            }
                            else
                                log.Info("Cannot process " + xeroEvent.EventCategory.ToString() + ", " + xeroEvent.EventType.ToString());
                        }
                    }));
                return req.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                log.Info(ex.GetType()?.ToString() + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        private static Dictionary<KeyValuePair<XeroHookCategory, XeroHookEvent>, Action<XeroEvent>> XeroActions
        {
            get
            {
                if (XeroHook.xeroActions == null)
                    XeroHook.initActions();
                return XeroHook.xeroActions;
            }
        }

        private static void initActions()
        {
            XeroHook.xeroActions = new Dictionary<KeyValuePair<XeroHookCategory, XeroHookEvent>, Action<XeroEvent>>();
            XeroHook.xeroActions[new KeyValuePair<XeroHookCategory, XeroHookEvent>(XeroHookCategory.Invoice, XeroHookEvent.Update)] = new Action<XeroEvent>(XeroHook.processInvoice);
            XeroHook.xeroActions[new KeyValuePair<XeroHookCategory, XeroHookEvent>(XeroHookCategory.Invoice, XeroHookEvent.Create)] = new Action<XeroEvent>(XeroHook.processInvoice);
        }

        private static void processInvoice(XeroEvent invoiceEvent)
        {
            if (invoiceEvent.EventCategory != XeroHookCategory.Invoice || invoiceEvent.EventType != XeroHookEvent.Update)
                return;
            QueueClient.CreateFromConnectionString(Configuration.SbConnectionString, "xerowebhook").Send(new BrokeredMessage((object)JsonConvert.SerializeObject((object)invoiceEvent)));
        }

        private static string HashData(string json, string h) => Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(h)).ComputeHash(Encoding.UTF8.GetBytes(json)));
    }
}
