using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FinanceIntegration.Model.Xero
{
    public class XeroResponseContact
    {
        [JsonPropertyName("ContactNumber")]
        public string ContactNumber { get; set; } = null;
        [JsonPropertyName("ContactID")]
        public string ContactID { get; set; } = null;
        [JsonPropertyName("Name")]

        public string Name { get; set; } = null;
    }
}
