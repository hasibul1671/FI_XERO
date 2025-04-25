/**
 * Copyright (c) 2017 Illuminance Solutions Pty Ltd. All rights reserved.
 * @name illumina_OnChange_SubCommPrg_Alert
 * @author Jarren Ong
 * @description Illuminance solutions ConsumerFile form functions 
 * @file illumina_OnChange_SubCommPrg_Alert.js
 * @requires XrmServiceToolkit.js
 * @version 1.1
 */

// Ensure base namespace exists
if (typeof (Illumi) == "undefined") {
  Illumi = {};
}

// Generate Namespace Objects  
Illumi.FundApplication = Illumi.FundApplication || {};
Illumi.FundApplication.Form = Illumi.FundApplication.Form || {};
;

Illumi.FundApplication.Form.OnChange_SubCommPrg_Alert = function () {
  //check the subcommprogs required criteria, if yes
  // check to see if contact has the subcomm prog
  // if false, alert
  try {
    console.log("On Change SubCommunityProg");
    //get the lookup, then the subcommprog entity and its criteria fields
    var subcommER = Xrm.Page.getAttribute("illumina_applicationformtype");
    retrievedSubComm = null;
    if (subcommER.getValue() != null) {
      var cols = ["illumina_criterianecessary", "illumina_communityprogramcriteria"];
      retrievedSubComm = XrmServiceToolkit.Soap.Retrieve(subcommER.getValue()[0].entityType, subcommER.getValue()[0].id, cols);
    }

    //compare the criterias of this and the subcommprog entity if criterianecessary = true
    if (retrievedSubComm.attributes["illumina_criterianecessary"].value == true && retrievedSubComm.attributes["illumina_communityprogramcriteria"] != null) {
      var subcommCriteria = retrievedSubComm.attributes["illumina_communityprogramcriteria"].id;

      var match = false;
      var quickViewSubgridRow = Xrm.Page.ui.quickForms.get("criteria_subgrid").getControl("criteria_subgrid").getGrid().getRows().getAll()
      quickViewSubgridRow.forEach(function (attribute) {
        var attributeKey = attribute.getKey();
        attributeKey = attributeKey.substring(1, attributeKey.length - 1);    //get rid of { and }
        attributeKey = attributeKey.toLowerCase();

        console.log("attrkey: ", attributeKey, "\nsubcommCriteria: ", subcommCriteria);
        console.log(attributeKey == subcommCriteria)

        if (attributeKey == subcommCriteria) {
          match = true;
          console.log(match)
        }
      });

      console.log("match: ", match)
      if (match == false) {
        window.alert("Contact does not have required criteria for this Sub-Community Program!")
      }
    }
  }
  catch (e) {
    console.log("error in OnChange_SubCommPrgAlert");
    //throw new Error("Error checking criteria");
  }
};

Illumi.FundApplication.Form.OnLoad = function () {
  if (Xrm.Page.ui.getFormType() == 1)
    Xrm.Page.getControl("header_process_illumina_requestedamount").getAttribute().setValue(0);
    
  Xrm.Page.getControl("header_process_illumina_requestedamount").setDisabled(true);

  Illumi.FundApplication.Form.DocumentSubgrid();
  Illumi.FundApplication.Form.AlertMessage();
  Xrm.Page.getAttribute("modifiedon").addOnChange(Illumi.FundApplication.Form.OnPostSave);
  Xrm.Page.getAttribute("illumina_requestedamount").addOnChange(Illumi.FundApplication.Form.CheckFundDetailForWarning);
  Illumi.FundApplication.Form.CheckFundDetailForWarning();
}

Illumi.FundApplication.Form.CheckFundDetailForWarning = function() {
  var relatedFundId = Xrm.Page.getAttribute("illumina_relatedfunddetail").getValue()[0].id;

  relatedFundId = relatedFundId.replace("{", "");
  relatedFundId = relatedFundId.replace("}", "");

  Xrm.WebApi.retrieveRecord("illumina_funddetails", relatedFundId, "$select=illumina_pendingallowancebalance").then(function (res) {
    if (res.illumina_pendingallowancebalance < 0 && Xrm.Page.getAttribute("illumina_validate").getValue() == false)
      Xrm.Page.ui.setFormNotification("There is possibility of an overdraft of $" + Math.abs(res.illumina_pendingallowancebalance) + ". Please check the related fund detail and all pending applications against it.", "WARNING");
    else 
      Xrm.Page.ui.clearFormNotification();
  },
  function (res) {
    console.log(res);
  });
}

//https://dynamisity.wordpress.com/2017/06/15/display-sharepoint-documents-in-a-subgrid-in-dynamics-365/
Illumi.FundApplication.Form.DocumentSubgrid = function () {
  if(Xrm.Page.ui.getFormType() != 1) {    //not create
    var recordId = Xrm.Page.data.entity.getId().replace(/[{}]/g, "");
    var iFrame = Xrm.Page.getControl("IFRAME_Documents");
    var currentFormId = Xrm.Page.ui.formSelector.getCurrentItem().getId();
    var oTypeCode = Xrm.Page.context.getQueryStringParameters().etc;
    if(iFrame != null) {
      //Build the url for document grid using the record id.
      var url = Xrm.Page.context.getClientUrl() + "/userdefined/areas.aspx?formid=" + currentFormId + "&inlineEdit=1&navItemName=Documents&oId=%7b" + recordId + "%7d&oType=" + oTypeCode + "&pagemode=iframe&rof=true&security=852023&tabSet=areaSPDocuments&theme=Outlook15White";
      //Sets the source url for IFrame
      Xrm.Page.getControl("IFRAME_Documents").setSrc(url);
      Xrm.Page.getControl("IFRAME_Documents").setVisible(true);
    }
  }
}

Illumi.FundApplication.Form.AlertMessage = function () {
  if(Xrm.Page.getAttribute("illumina_alert").getValue() != null) {
    Xrm.Page.ui.setFormNotification(Xrm.Page.getAttribute("illumina_alert").getValue(), "WARNING", "01");
  }
  else {
    Xrm.Page.ui.clearFormNotification("01");
  }
}

Illumi.FundApplication.Form.OnPostSave = function (executionContext) {
  var saveEvent = executionContext.getEventArgs();
  var formContext = executionContext.getFormContext();

  if (Xrm.Page.getAttribute("illumina_validate").getValue() == true) {
    // saveEvent.preventDefault();
    // formContext.data.save().then(function () {
    //   formContext.data.refresh().then(Illumi.FundApplication.Form.AlertMessage());
    // }, function (res) {console.log(res);});
  }
}

Illumi.FundApplication.Form.SetFocus = function () {
	Xrm.Page.ui.tabs.get("Summary").setFocus();
}