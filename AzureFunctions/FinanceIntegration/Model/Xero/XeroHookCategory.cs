// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroHookCategory
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using System.Runtime.Serialization;

namespace FinanceIntegration.Model.Xero
{
    public enum XeroHookCategory
    {
        [EnumMember(Value = "CONTACT")] Contact,
        [EnumMember(Value = "INVOICE")] Invoice,
    }
}
