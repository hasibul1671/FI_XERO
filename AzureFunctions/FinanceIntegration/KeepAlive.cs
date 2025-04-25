// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.KeepAlive
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;

namespace FinanceIntegration
{
    public static class KeepAlive
    {
        [FunctionName("KeepAlive")]
        public static void Run([TimerTrigger("0 */15 * * * *")] TimerInfo myTimer, TraceWriter log) => log.Info(string.Format("C# Timer trigger function executed at: {0}", (object)DateTime.Now));
    }
}
