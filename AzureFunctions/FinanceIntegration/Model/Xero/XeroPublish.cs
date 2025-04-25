// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroPublish
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Newtonsoft.Json;
using System.Collections.Generic;

namespace FinanceIntegration.Model.Xero
{
    public class XeroPublish
    {
        [JsonProperty("events")]
        public List<XeroEvent> Events;
        [JsonProperty("lastEventSequence")]
        public int LastEventSequence;
        [JsonProperty("firstEventSequence")]
        public int FirstEventSequence;
        [JsonProperty("entropy")]
        public string Entropy;
    }
}
