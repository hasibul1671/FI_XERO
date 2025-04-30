using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static FinanceIntegration.Model.Dynamics.PurchaseOrder;

namespace FinanceIntegration.Model.Dynamics
    {
    public class PayRun
        {
        public const string LogicalName = "illumina_payrun";
        public const string LogicalPluralName = "illumina_payruns";

        public static readonly Cds.Statuses Statuses = new Cds.Statuses {
            new Cds.Status { Value = 0, Label = "Active",   Reasons = new List<Cds.StatusReason> { new Cds.StatusReason { Value = 1, Label = "Active" } } },
            new Cds.Status { Value = 1, Label = "Inactive", Reasons = new List<Cds.StatusReason> { new Cds.StatusReason { Value = 2, Label = "Inactive" } } },
        };
        public enum PayCalendarPeriodType
            {
            Weekly = 901010000,
            Fortnightly = 901010001,
            Monthly = 901010002,
            }
        public enum PayRunStatusType
            {
            Draft = 901010000,
            XeroPosted = 901010001,
            None = 901010002,
            SentToXeroError = 901010003,
            XeroDraft = 901010004,
            }
        public const string Selects = "illumina_payrunid,illumina_name,statecode,statuscode,illumina_payrunstatus, " +
            "illumina_startpayperiod,illumina_endpayperiod,illumina_payperiod, illumina_paymentdate, illumina_externalpayrunid,illumina_paycalendarperiod ";
        public const string Expands = "illumina_timeentry_payrun_illumina_payrun,illumina_taskallowance_payrun_payrun($expand=illumina_projecttask)";

        [JsonIgnore]
        public Guid? Id { get; set; } = null;
        [JsonPropertyName("illumina_payrunid")]
        public Guid? PayRunId { set { Id = value; } }
        [JsonPropertyName("illumina_name")]
        public string Name { get; set; } = null;
        [JsonPropertyName("statecode")]
        public int? State { get; set; } = null;
        [JsonPropertyName("statuscode")]
        public int? StatusReason { get; set; } = null;
        [JsonPropertyName("illumina_payrunstatus")]
        public PayRunStatusType? PayRunStatus { get; set; } = null;

        [JsonPropertyName("illumina_startpayperiod")]
        public DateTime? StartPayPeriod { get; set; } = null;
        [JsonPropertyName("illumina_endpayperiod")]
        public DateTime? EndPayPeriod { get; set; } = null;
        [JsonPropertyName("illumina_payperiod")]
        public string PayPeriod { get; set; } = null;
        [JsonPropertyName("illumina_paymentdate")]
        public DateTime? PaymentDate { get; set; } = null;
        [JsonPropertyName("illumina_externalpayrunid")]
        public string ExternalPayrunId { get; set; } = null;
        [JsonPropertyName("illumina_paycalendarperiod")]
        public PayCalendarPeriodType? PayCalendarPeriod { get; set; } = null;

        [JsonPropertyName("illumina_timeentry_payrun_illumina_payrun")]
        public List<TimeEntry> TimeEntries { get; set; } = null;

        [JsonPropertyName("illumina_taskallowance_payrun_payrun")]
        public List<JobAllowance> JobAllowances { get; set; } = null;


        #region

        public static PayRun Open(Guid? id, Cds.Client client)
            {
            if (id == null) throw new ArgumentException("Must supply a record id", nameof(id));
            string q = $"{LogicalPluralName}({id})?$select={Selects}";
            if (!string.IsNullOrWhiteSpace(Expands))
                q = $"{q}&$expand={Expands}";
            return Cds.Entity<PayRun>.Open(q, client);
            }

        //public static List<PayRun> GetNonPostedPayRuns(Cds.Client client)
        //{
        //    string q = $"{LogicalPluralName}?$filter=illumina_payrunstatus ne {(int)PayRunStatusType.XeroPosted} and statecode eq 0 ";
        //    return Cds.Entities<PayRun>.Open(q, client);
        //}

        public static List<PayRun> GetXeroDraftPayRuns(Cds.Client client)
            {
            string q = $"{LogicalPluralName}?$filter=illumina_payrunstatus eq {(int)PayRunStatusType.XeroDraft} and statecode eq 0 ";
            if (!string.IsNullOrWhiteSpace(Expands))
                q = $"{q}&$expand={Expands}";
            return Cds.Entities<PayRun>.Open(q, client);
            }

        public bool Save(Cds.Client client)
            {
            Cds.Entity<PayRun> cds = new Cds.Entity<PayRun>(this, LogicalPluralName);
            PayRun result = Id.HasValue ? cds.Update(Id.Value, client) : cds.Create(client);
            Id = result?.Id;
            return result != null;
            }

        #endregion
        }
    }


