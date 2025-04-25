// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroEvent
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace FinanceIntegration.Model.Xero
{
    public class XeroEvent
    {
        [JsonProperty("resourceUrl")]
        public string ResourceUrl;
        [JsonProperty("resourceId")]
        public Guid ResourceId;
        [JsonProperty("eventDateUtc")]
        public DateTime EventDateUtc;
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("eventType")]
        public XeroHookEvent EventType;
        [JsonProperty("eventCategory")]
        public XeroHookCategory EventCategory;
        [JsonProperty("tenantId")]
        public Guid TenantId;
        [JsonProperty("tenantType")]
        public XeroTenantType TenantType;
    }
}
