// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.ConverterPayload
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Newtonsoft.Json;

namespace FinanceIntegration.Model.Dynamics
{
    internal class ConverterPayload
    {
        [JsonProperty("filename")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("base64content")]
        public string Content { get; set; } = string.Empty;
    }
}
