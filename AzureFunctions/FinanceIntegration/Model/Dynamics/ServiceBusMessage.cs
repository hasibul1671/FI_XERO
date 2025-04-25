// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.ServiceBusMessage
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.ServiceBus.Messaging;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FinanceIntegration.Model.Dynamics
{
    public class ServiceBusMessage
    {
        private static readonly string keyRoot = "http://schemas.microsoft.com/xrm/2011/Claims";
        private static readonly string keyEntityLogicalName = nameof(EntityLogicalName);
        private static readonly string keyRequestName = nameof(RequestName);

        public string EntityLogicalName { get; set; } = string.Empty;

        public string RequestName { get; set; } = string.Empty;

        public Entity Target { get; set; }

        public Guid PrimaryEntityId { get; set; } = Guid.Empty;

        public string PrimaryEntityName { get; set; } = string.Empty;

        public string MessageName { get; set; } = string.Empty;

        public Guid InitiatingUserId { get; set; } = Guid.Empty;

        public List<KeyValuePair<string, Entity>> PreImages { get; set; } = new List<KeyValuePair<string, Entity>>();

        public ServiceBusMessage(BrokeredMessage message)
        {
            if (message == null)
                return;
            this.EntityLogicalName = message.Properties.ContainsKey(ServiceBusMessage.keyRoot + "/" + ServiceBusMessage.keyEntityLogicalName) ? message.Properties[ServiceBusMessage.keyRoot + "/" + ServiceBusMessage.keyEntityLogicalName].ToString() : string.Empty;
            this.RequestName = message.Properties.ContainsKey(ServiceBusMessage.keyRoot + "/" + ServiceBusMessage.keyRequestName) ? message.Properties[ServiceBusMessage.keyRoot + "/" + ServiceBusMessage.keyRequestName].ToString() : string.Empty;
            RemoteExecutionContext executionContext = (RemoteExecutionContext)null;
            if (message.ContentType == "application/msbin1")
                executionContext = message.GetBody<RemoteExecutionContext>();
            else if (message.ContentType == "application/json")
            {
                executionContext = message.GetBody<RemoteExecutionContext>((XmlObjectSerializer)new DataContractJsonSerializer(typeof(RemoteExecutionContext)));
            }
            else
            {
                int num = message.ContentType == "application/xml" ? 1 : 0;
            }
            this.PrimaryEntityId = executionContext.PrimaryEntityId;
            this.PrimaryEntityName = executionContext.PrimaryEntityName;
            this.MessageName = executionContext.MessageName;
            if (executionContext.InitiatingUserId != Guid.Empty)
                this.InitiatingUserId = executionContext.InitiatingUserId;
            if (executionContext.InputParameters.ContainsKey(nameof(Target)) && executionContext.InputParameters[nameof(Target)].GetType() == typeof(Entity))
                this.Target = (Entity)executionContext.InputParameters[nameof(Target)];
            if (executionContext.PreEntityImages.Count <= 0)
                return;
            this.PreImages = executionContext.PreEntityImages.ToList<KeyValuePair<string, Entity>>();
        }
    }
}
