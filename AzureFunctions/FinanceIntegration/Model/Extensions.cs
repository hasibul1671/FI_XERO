// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Extensions
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Newtonsoft.Json.Linq;

namespace FinanceIntegration.Model
{
    public static class Extensions
    {
        public static void FixEntityReference(this JObject jObject, string fieldName) => ((JObject)jObject[fieldName]).Remove("KeyAttributes");
    }
}
