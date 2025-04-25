// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroItem
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using FinanceIntegration.Model.Dynamics;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xero.Api.Core.Model;

namespace FinanceIntegration.Model.Xero
{
    public static class XeroItem
    {
        public static List<LineItem> BuildItems(
          EntityCollection disbursementEC,
          XeroTrackingCode xeroTrackingCode,
          TraceWriter log)
        {
            log.Info("Entered BuildItems - Build JSON for API call");
            List<LineItem> lineItemList = new List<LineItem>();
            foreach (Entity entity1 in (Collection<Entity>)disbursementEC.Entities)
            {
                ItemTracking itemTracking = new ItemTracking();
                ItemTrackingCategory trackingCategory1 = new ItemTrackingCategory()
                {
                    Name = xeroTrackingCode.TrackingCategory,
                    Option = xeroTrackingCode.TrackingOption
                };
                itemTracking.Add(trackingCategory1);
                LineItem lineItem = new LineItem();
                WebProxyClient webProxyClient = new WebProxyClient();
                foreach (KeyValuePair<string, object> attribute in (DataCollection<string, object>)entity1.Attributes)
                {
                    if (attribute.Value != null)
                    {
                        switch (attribute.Key)
                        {
                            case "illumina_description":
                                lineItem.Description = attribute.Value.ToString();
                                break;
                            case "illumina_distributionpolicyitem":
                                EntityReference entityReference1 = (EntityReference)attribute.Value;
                                log.Info("Getting Product Information");
                                if (entityReference1 == null)
                                    throw new InvalidPluginExecutionException("Distribution Policy Item on Disbursement is Empty.");
                                Entity entity2 = webProxyClient.Retrieve(entityReference1.LogicalName, entityReference1.Id, new ColumnSet(new string[2]
                                {
                  "illumina_name",
                  "illumina_glcode"
                                }));
                                lineItem.ItemCode = entity2.GetAttributeValue<string>("illumina_glcode") != null ? entity2.GetAttributeValue<string>("illumina_glcode") : throw new InvalidPluginExecutionException("GL Code on Distribution Policy Item is Empty.");
                                break;
                            case "illumina_unitprice":
                                lineItem.UnitAmount = new Decimal?(entity1.GetAttributeValue<Money>(attribute.Key).Value);
                                break;
                            case "illumina_tax":
                                lineItem.TaxAmount = new Decimal?(entity1.GetAttributeValue<Money>(attribute.Key).Value);
                                break;
                            case "illumina_taxable":
                                lineItem.TaxType = !entity1.GetAttributeValue<bool>(attribute.Key) ? "EXEMPTEXPENSES" : "INPUT";
                                break;
                            case "illumina_distributionpolicy":
                                EntityReference entityReference2 = (EntityReference)attribute.Value;
                                EntityReference attributeValue = webProxyClient.Retrieve(entityReference2.LogicalName, entityReference2.Id, new ColumnSet(new string[2]
                                {
                  "illumina_name",
                  "illumina_subfund"
                                })).GetAttributeValue<EntityReference>("illumina_subfund");
                                Entity entity3 = webProxyClient.Retrieve(attributeValue.LogicalName, attributeValue.Id, new ColumnSet(new string[1]
                                {
                  "illumina_name"
                                }));
                                if (entity3 != null)
                                {
                                    ItemTrackingCategory trackingCategory2 = new ItemTrackingCategory()
                                    {
                                        Name = "Sub Fund",
                                        Option = entity3.GetAttributeValue<string>("illumina_name")
                                    };
                                    itemTracking.Add(trackingCategory2);
                                    break;
                                }
                                break;
                        }
                    }
                    lineItem.Quantity = new Decimal?((Decimal)1);
                    lineItem.Tracking = itemTracking;
                }
                lineItemList.Add(lineItem);
            }
            return lineItemList;
        }
    }
}
