/**
 * Copyright (c) 2017 Illuminance Solutions Pty Ltd. All rights reserved.
 * @name illumina_ShowandhidetabsJS.js
 * @author Kusum Khatkar, Jarren Ong
 * @description Illuminance solutions clear lookup for community program criteria  
 * @file illumina_ShowandhidetabsJS.js
 * @requires XrmServiceToolkit.js
 * @version 1.1
**/

// Ensure base namespace exists
if (typeof (Illumi) == "undefined") {
	Illumi = {};
}

// Generate Namespace Objects  
Illumi.Case = Illumi.Case || {};
Illumi.Case.Form = Illumi.Case.Form || {};


Illumi.Case.Form.ShowHideOnLoad= function(){

    //console.log(Xrm.Page.getAttribute("illumina_applicationformtype").getValue()[0].name);

    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Carers Assistance").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Education Assistance").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Funeral Assistance").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Language,Culture,Heritage").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Lore and Culture").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Medical Assistance").setVisible(false);
    Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Economic Development").setVisible(false);
    console.log("Hide tabs");

 if(Xrm.Page.getAttribute("illumina_applicationformtype").getValue() == null)
    {
       // alert("I am in new 3");
        return;
    }

    if(Xrm.Page.getAttribute("illumina_applicationformtype").getValue()[0].name != null )
    {
        var FormTypeValue = Xrm.Page.getAttribute("illumina_applicationformtype").getValue()[0].name;
       
        if(FormTypeValue == "Carer Assistance")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Carers Assistance").setVisible(true);
        }
        else if(FormTypeValue == "Education Assistance (High School & Tertiary)")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Education Assistance").setVisible(true);
        }
           else if(FormTypeValue == "Education Assistance (Pre-High School)")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Education Assistance").setVisible(true);
        }
        else if(FormTypeValue == "Funeral Assistance")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Funeral Assistance").setVisible(true);
        }


        else if(FormTypeValue == "Language and Culture")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Language,Culture,Heritage").setVisible(true);
        }

        else if(FormTypeValue == "Lore and Culture")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Lore and Culture").setVisible(true);
        }
        else if(FormTypeValue == "Medical Assistance")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Medical Assistance").setVisible(true);
        }

        else if(FormTypeValue == "Economic Development")
        {
            Xrm.Page.ui.tabs.get("tab_Generic").sections.get("Details_Economic Development").setVisible(true);
		}
    }
}
