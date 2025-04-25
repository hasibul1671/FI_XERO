/**
 * Copyright (c) 2017 Illuminance Solutions Pty Ltd. All rights reserved.
 * @name illumina_Contact_Scripts
 * @author Jarren Ong
 * @description Illuminance solutions Contact form functions 
 * @file Illumina_Contact_Scripts.js
 * @version 1.1
 */

// Ensure base namespace exists
if(typeof (Illumi) === "undefined")
	Illumi = {};

// Generate Namespace Objects  
Illumi.Contact = Illumi.Contact || {};
Illumi.Contact.Form = Illumi.Contact.Form || {};
;

//------------------------------------------------------------------------------------------------------------------------------
Illumi.Contact.Form.OnLoad = function () {
	Illumi.Contact.Form.AlertMessage();
}

Illumi.Contact.Form.OnSave = function () {
}
//------------------------------------------------------------------------------------------------------------------------------

Illumi.Contact.Form.AlertMessage = function () {
	if(Xrm.Page.getAttribute("illumina_alert") != null) {
		if(Xrm.Page.getAttribute("illumina_alert").getValue() != null) {
			Xrm.Page.ui.setFormNotification(Xrm.Page.getAttribute("illumina_alert").getValue(), "WARNING", "01")
		}
		else {
			Xrm.Page.ui.clearFormNotification("01");
		}
	}
}

Illumi.Contact.Form.WarningOnInactive = function () {
	if(Xrm.Page.getAttribute("statecode") != null){
		if(Xrm.Page.getAttribute("statecode").getValue() == 1){
			window.alert("Record is inactive.");
		}
	}
}
