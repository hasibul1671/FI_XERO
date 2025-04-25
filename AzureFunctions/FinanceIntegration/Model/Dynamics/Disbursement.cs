// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.Disbursement
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FinanceIntegration.Model.Dynamics
{
    public static class Disbursement
    {
        public static EntityCollection getDisbursements(Guid purchaseOrder)
        {
            WebProxyClient webProxyClient = new WebProxyClient();
            QueryExpression query = new QueryExpression("illumina_lineitem");
            query.ColumnSet.AddColumns("illumina_description", "illumina_distributionpolicyitem", "illumina_distributionpolicy", "illumina_amount", "illumina_taxable", "illumina_unitprice", "illumina_tax");
            query.Criteria.AddCondition("illumina_purchaseorderid", ConditionOperator.Equal, (object)purchaseOrder);
            EntityCollection disbursements = webProxyClient.RetrieveMultiple((QueryBase)query);
            if (disbursements.Entities.Count != 0)
                return disbursements;
            throw new SystemException("No Disbursements?");
        }
    }
}
