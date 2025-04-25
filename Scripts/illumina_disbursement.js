Illumi = {}

if(typeof(Illumi) === null)
	Illumi = {}

Illumi.Disbursement = {}

Illumi.Disbursement.OnLoad = function () {
	Xrm.Page.getAttribute("illumina_distributionpolicyitem").addOnChange(Illumi.Disbursement.MapDpItem);
}

Illumi.Disbursement.MapDpItem = function () {
	var dpItem = Xrm.Page.getAttribute("illumina_distributionpolicyitem").getValue();

	if (dpItem == null) {
		console.log("nanimo nakatta");
		return;
	}

	var itemId = dpItem[0].id;
	itemId = itemId.replace("{", "");
	itemId = itemId.replace("}", "");
	var entityType = dpItem[0].entityType;
	var selects = "$select=illumina_name";

	Xrm.WebApi.retrieveRecord(entityType, itemId, selects).then(function success(res) {
		var itemName = res.illumina_name;

		Xrm.Page.getAttribute("illumina_name").setValue(itemName);
	},
	function error(res) {
		console.log(res.message);
	});
}