using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IlluminanceSolutions.Utility;

namespace IlluminanceSolutions {
    public sealed class CollateDisbursements : CodeActivity {
        protected override void Execute(CodeActivityContext context) {
            ITracingService tracingService = null;
            try {
                // Get tracingservice
                tracingService = context.GetExtension<ITracingService>();

                // Setting up usual Custom Workflow Properties
                IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
                IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
                IOrganizationService orgService = serviceFactory.CreateOrganizationService(workflowContext.UserId);

                tracingService.Trace("Custom Workflow {0} has started.", this.GetType().Name);

                Entity triggerEntity = orgService.Retrieve(workflowContext.PrimaryEntityName, workflowContext.PrimaryEntityId,
                    new ColumnSet(
                        "illumina_applicationformtype",
                        "illumina_approved"));

                if(!triggerEntity.GetAttributeValue<bool>("illumina_approved")) {
                    tracingService.Trace("Fund application not approved");
                    throw new InvalidPluginExecutionException("Fund application not approved");
                }

                Entity distroPolicyEntity = orgService.Retrieve(triggerEntity.GetAttributeValue<EntityReference>("illumina_applicationformtype").LogicalName,
                    triggerEntity.GetAttributeValue<EntityReference>("illumina_applicationformtype").Id, new ColumnSet("illumina_xerotrackingcodeid"));

                if (!distroPolicyEntity.Contains("illumina_xerotrackingcodeid")) {
                    tracingService.Trace("No Xero Tracking Found - END");
                    throw new InvalidPluginExecutionException("No Xero Tracking Found");
                }

                tracingService.Trace("Passed Approval and Xero Tracking Check");

                var fetchData = new {
                    illumina_fundapplication = workflowContext.PrimaryEntityId,
                    statecode = "0"
                };

                var fetchXml = $@"
                    <fetch aggregate='true'>
                        <entity name='illumina_lineitem'>
                            <attribute name='illumina_supplier' alias='Supplier' groupby='true' />
                            <filter type='and'>
                                <condition attribute='illumina_fundapplication' operator='eq' value='{fetchData.illumina_fundapplication}'/>
                                <condition attribute='illumina_purchaseorderid' operator='null' />
                                <condition attribute='statecode' operator='eq' value='{fetchData.statecode}'/>
                            </filter>
                        </entity>
                    </fetch>";

                tracingService.Trace(fetchXml);

                EntityCollection supplierEC = orgService.RetrieveMultiple(new FetchExpression(fetchXml));

                if (supplierEC.Entities.Count == 0) {
                    tracingService.Trace("No suppliers found - END");
                    return;
                }

                tracingService.Trace("Suppliers found = " + supplierEC.Entities.Count);

                foreach (var supplier in supplierEC.Entities) {
                    EntityReference suppER = ((EntityReference)((AliasedValue)supplier["Supplier"]).Value);
                    tracingService.Trace("Checking Supplier - " + suppER.Name);
                    Entity supplierEntity = orgService.Retrieve(suppER.LogicalName, suppER.Id, new ColumnSet("name","emailaddress1"));
                    if (!supplierEntity.Contains("emailaddress1")) {
                        tracingService.Trace("No Email Address Found - END");
                        throw new InvalidPluginExecutionException("No Email Address Found for Supplier - " + supplierEntity.GetAttributeValue<string>("name") ?? "");
                    }
                }

                tracingService.Trace("Passed E-mail checks for suppliers");

                // Guid of PO and supplier
                Dictionary<Guid, Guid> keyDict = new Dictionary<Guid, Guid>();

                foreach (var supplier in supplierEC.Entities) {
                    EntityReference suppER = ((EntityReference)((AliasedValue)supplier["Supplier"]).Value);
                    Entity supplierEntity = orgService.Retrieve(suppER.LogicalName, suppER.Id, new ColumnSet("name", "emailaddress1", "illumina_collatedisbursements"));
                    EntityCollection dispEC = getDisbursements(workflowContext, orgService, suppER);

                    if (supplierEntity.GetAttributeValue<bool>("illumina_collatedisbursements"))
                    {
                        EntityReference purchaseOrder = createPurchaseOrder(orgService,
                                tracingService,
                                triggerEntity.ToEntityReference(),
                                ((EntityReference)((AliasedValue)supplier["Supplier"]).Value),
                                distroPolicyEntity.GetAttributeValue<EntityReference>("illumina_xerotrackingcodeid"));

                        foreach (var ent in dispEC.Entities)
                        {
                            addDisbursementToPO(orgService,
                                tracingService,
                                ent.ToEntityReference(),
                                purchaseOrder);
                        }

                        keyDict.Add(purchaseOrder.Id, suppER.Id);
                    }
                    else
                    {
                        foreach (var ent in dispEC.Entities)
                        {
                            EntityReference purchaseOrder = createPurchaseOrder(orgService,
                                tracingService,
                                triggerEntity.ToEntityReference(),
                                ((EntityReference)((AliasedValue)supplier["Supplier"]).Value),
                                distroPolicyEntity.GetAttributeValue<EntityReference>("illumina_xerotrackingcodeid"));

                            addDisbursementToPO(orgService,
                                tracingService,
                                ent.ToEntityReference(),
                                purchaseOrder);

                            keyDict.Add(purchaseOrder.Id, suppER.Id);
                        }
                    }
                }

                foreach (var po in keyDict) {
                    setPOForDelivery(orgService,
                        tracingService,
                        new EntityReference("illumina_purchaseorder", po.Key));
                }

            } catch (Exception e) {
                for (var ie = e.InnerException; ie != null; ie = ie.InnerException)
                    tracingService?.Trace(e.GetType() + ": " + e.Message);
                // TODO: put the good one here
                throw new InvalidPluginExecutionException(e.GetType() + ": " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }

        private static EntityCollection getDisbursements(IWorkflowContext workflowContext, IOrganizationService orgService, EntityReference suppER)
        {
            QueryExpression disbursementQuery = new QueryExpression("illumina_lineitem");
            disbursementQuery.ColumnSet.AddColumns("illumina_name", "illumina_supplier");
            disbursementQuery.Criteria.AddCondition("illumina_fundapplication", ConditionOperator.Equal, workflowContext.PrimaryEntityId);
            disbursementQuery.Criteria.AddCondition("illumina_purchaseorderid", ConditionOperator.Null);
            disbursementQuery.Criteria.AddCondition("illumina_supplier", ConditionOperator.Equal, suppER.Id);
            disbursementQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            EntityCollection dispEC = orgService.RetrieveMultiple(disbursementQuery);
            return dispEC;
        }

        public EntityReference createPurchaseOrder(IOrganizationService orgService, ITracingService tracingService, EntityReference fundApplication, EntityReference supplier, EntityReference xeroTrackingCode) {
            tracingService.Trace("Creating PO for " + supplier.Name);
            Entity purchaseOrder = new Entity("illumina_purchaseorder");
            purchaseOrder["illumina_fundapplicationid"] = fundApplication;
            purchaseOrder["illumina_supplierid"] = supplier;
            purchaseOrder["illumina_xerotrackingcodeid"] = xeroTrackingCode;
            purchaseOrder["illumina_purchaseorderdate"] = DateTime.UtcNow;
            purchaseOrder["illumina_purchaseorderexpiry"] = DateTime.UtcNow.AddDays(7);
            tracingService.Trace("Attempt to create PO - NOW");
            purchaseOrder.Id = orgService.Create(purchaseOrder);
            return new EntityReference(purchaseOrder.LogicalName, purchaseOrder.Id);
        }

        public void addDisbursementToPO(IOrganizationService orgService, ITracingService tracingService, EntityReference disbursementToAdd, EntityReference purchaseOrder) {
            tracingService.Trace("Adding Disbursement " + disbursementToAdd.Name);
            Entity updateEntity = new Entity(disbursementToAdd.LogicalName, disbursementToAdd.Id);
            updateEntity["illumina_purchaseorderid"] = purchaseOrder;
            orgService.Update(updateEntity);
            tracingService.Trace("Added Disbursement to" + purchaseOrder.Name);
        }

        public void setPOForDelivery(IOrganizationService orgService, ITracingService tracingService, EntityReference purchaseOrder) {
            tracingService.Trace("Setting PO to Awaiting Delivery " + purchaseOrder.Name);
            decimal relatedPODetailTotal = ReCalcPO.CaluculateValues(orgService, tracingService, "illumina_lineitem", purchaseOrder, "illumina_unitprice");
            decimal relatedPOTax = ReCalcPO.CaluculateValues(orgService, tracingService, "illumina_lineitem", purchaseOrder, "illumina_tax");
            decimal relatedPOTotal = ReCalcPO.CaluculateValues(orgService, tracingService, "illumina_lineitem", purchaseOrder, "illumina_amount");
            Entity updateEntity = new Entity(purchaseOrder.LogicalName, purchaseOrder.Id);
            updateEntity["illumina_purchaseorderstatus"] = new OptionSetValue(390950001); //Awaiting Delivery
            updateEntity["illumina_detailamount"] = new Money(relatedPODetailTotal);
            updateEntity["illumina_totaltax"] = new Money(relatedPOTax);
            updateEntity["illumina_totalamount"] = new Money(relatedPOTotal);
            orgService.Update(updateEntity);
            tracingService.Trace("Updated PO " + purchaseOrder.Name);
        }
    }
}
