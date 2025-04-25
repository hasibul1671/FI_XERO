/**
 * Copyright (c) 2017 Illuminance Solutions Pty Ltd
 * Authors: Sebastian Southen, Samuel Warnock
 *
 * NOTE: Please copy this class to the Dynamics Plugin Solutions, add IAutoNumber as a Link.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace IlluminanceSolutions {
	/// <summary>
	/// Plugin Registration:
	///   Primary Entity: *
	/// </summary>
	public class Autonumber : IAutonumber {
		public Autonumber(string unsecure, string secure) : base(unsecure, secure) { }

		// Return the field to write the new number to, or null if not to write a number
		public override string GetFieldString(Entity target, Entity image) {
			// Target contains all the fields that are changing (created or updated)
			// Image contains all the fields selected when setup in the Plugin Reg Tool (pre is before changes made in update, post is including update changes)

			// TODO: test target & image, if conditions met return attribute name
			if (image.LogicalName == "illumina_fundapplication") {
				return "illumina_applicationid";        //whole number
			}
			else if (image.LogicalName == "contact") {
				return "illumina_contactid";
			}
            else if (image.LogicalName == "illumina_purchaseorder") {
                return "illumina_purchaseordernumber";
            }
            else
			return null;
		}

		public override void PostNumberingCallback(Entity target, Entity image, int number) {
			if (image.LogicalName == "illumina_fundapplication") {
				target["illumina_name"] = number.ToString();
			} else if (image.LogicalName == "illumina_purchaseorder") {
                target["illumina_name"] = "MS-"+number.ToString();
            }

        }
	}
}
	

