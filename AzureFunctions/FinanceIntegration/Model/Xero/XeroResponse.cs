using System;
using System.Collections.Generic;
using Xero.Api.Core.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Xero.Api.Core.Model.Types;

namespace FinanceIntegration.Model.Xero
{
    public class XeroResponse
    {
        [JsonPropertyName("Invoices")]
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();
        [JsonPropertyName("Contacts")]
        public List<XeroResponseContact> Contacts { get; set; } = new List<XeroResponseContact>();


    }
}
