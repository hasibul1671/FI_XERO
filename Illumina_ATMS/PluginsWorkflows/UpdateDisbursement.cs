using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IlluminanceSolutions {
	public class UpdateDisbursement : IPlugin {
		ITracingService tracingService;
		public void Execute(IServiceProvider serviceProvider) {
			try {
				tracingService = serviceProvider.GetService(typeof(ITracingService)) as ITracingService;
				IPluginExecutionContext pluginContext = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;

				tracingService.Trace($"{this.GetType()}. Execution Start.");

				if (pluginContext.InputParameters.Contains("Target")) {

					Entity targetEntity = (Entity)pluginContext.InputParameters["Target"];
                    Entity preImageEntity = null;

                    if (pluginContext.MessageName == "Update") {
                        preImageEntity = pluginContext.PreEntityImages["preImage"];
                    }

                    bool tax;

                    if (pluginContext.MessageName == "Create") {
                        tax = targetEntity.GetAttributeValue<bool>("illumina_taxable");
                    }
                    else if (pluginContext.MessageName == "Update") {
                        if (!targetEntity.Contains("illumina_taxable")) {
                            tax = preImageEntity.GetAttributeValue<bool>("illumina_taxable");
                        } else {
                            tax = targetEntity.GetAttributeValue<bool>("illumina_taxable");
                        }
                        if (!targetEntity.Contains("illumina_amount")) {
                            targetEntity["illumina_amount"] = preImageEntity.GetAttributeValue<Money>("illumina_amount");
                        }
                    } else {
                        tracingService.Trace("??");
                        return;
                    }

					if (targetEntity.LogicalName == "illumina_lineitem") {
						if (targetEntity.Contains("illumina_amount")) {
									if (targetEntity.GetAttributeValue<Money>("illumina_amount").Value == 0) {
										tracingService.Trace("Amount is Zero, cant divide zero, zero it out");
                                        targetEntity["illumina_unitprice"] = new Money(0);
                                        targetEntity["illumina_tax"] = new Money(0);
                                        tracingService.Trace("Disbursement - ZERO - Execution End");
                                        return;
									}
									if (tax) {
										tracingService.Trace("Disbursement is taxable");
										decimal exGST = Math.Round(targetEntity.GetAttributeValue<Money>("illumina_amount").Value / (decimal)1.1,4);
										tracingService.Trace("Ex GST Amount = " + exGST);
										targetEntity["illumina_unitprice"] = new Money(exGST);
										targetEntity["illumina_tax"] = new Money(targetEntity.GetAttributeValue<Money>("illumina_amount").Value - exGST);
										tracingService.Trace("Disbursement - TAX - Execution End");
									} else {
										tracingService.Trace("Disbursement is tax exempt");
										targetEntity["illumina_unitprice"] = targetEntity.GetAttributeValue<Money>("illumina_amount");
										targetEntity["illumina_tax"] = new Money(0);
										tracingService.Trace("Disbursement - TAX EXEMPT - Execution End");
									}
								} else {
									tracingService.Trace("Disbursement has no amount.");
								}
						}
						else {
						tracingService.Trace("Plugin fired on " + targetEntity.LogicalName + " - Execution End");
					}
				} else {
					tracingService.Trace("No Target and/or not Entity?");
				}
			} catch (Exception e) {
				for(var ie = e.InnerException; ie != null; ie = ie.InnerException)
					tracingService?.Trace(e.GetType() + ": " + e.Message);
				throw new InvalidPluginExecutionException(e.GetType() + ": " + e.Message + Environment.NewLine + e.StackTrace);
			}
		}
	}
}
