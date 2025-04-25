// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.XeroTrackingCode
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FinanceIntegration.Model.Dynamics
{
    public class XeroTrackingCode
    {
        public Guid Id { get; set; } = Guid.Empty;

        public string TrackingOption { get; set; } = string.Empty;

        public string TrackingCategory { get; set; }

        public XeroTrackingCode(Guid id)
        {
            Entity entity = !(id == Guid.Empty) ? new WebProxyClient().Retrieve("illumina_xerotrackingcode", id, new ColumnSet(new string[2]
            {
        "illumina_name",
        "illumina_trackingcategory"
            })) : throw new Exception("Invalid id - Xero Tracking Code");
            this.Id = entity != null ? entity.Id : throw new Exception("Invalid Xero Tracking Code returned.");
            if (entity.Attributes.ContainsKey("illumina_name"))
                this.TrackingOption = (string)entity.Attributes["illumina_name"];
            if (!entity.Attributes.ContainsKey("illumina_trackingcategory"))
                return;
            this.TrackingCategory = entity.FormattedValues["illumina_trackingcategory"];
        }
    }
}
