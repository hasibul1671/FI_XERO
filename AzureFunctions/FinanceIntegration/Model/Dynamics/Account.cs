// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.Account
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FinanceIntegration.Model.Dynamics
{
    public class Account
    {
        public Guid Id { get; set; } = Guid.Empty;

        public string XeroId { get; set; } = string.Empty;

        public EntityReference ParentAccount { get; set; }

        public string Name { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        public string AddressLine3 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ABN { get; set; } = string.Empty;

        public string ACN { get; set; } = string.Empty;

        public string BankAccountName { get; set; } = string.Empty;

        public string BSBNumber { get; set; } = string.Empty;

        public string BankAccountNumber { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public Account(Guid id)
        {
            Entity entity = !(id == Guid.Empty) ? new WebProxyClient().Retrieve("account", id, new ColumnSet(new string[18]
            {
        "name",
        "illumina_xeroid",
        "parentaccountid",
        "emailaddress1",
        "address1_line1",
        "address1_line2",
        "address1_line3",
        "address1_city",
        "address1_stateorprovince",
        "address1_postalcode",
        "address1_country",
        "telephone1",
        "illumina_abn",
        "illumina_acn",
        "illumina_bankaccountname",
        "illumina_bsbnumber",
        "illumina_bankaccountnumber",
        "illumina_reference"
            })) : throw new Exception("Invalid id.");
            this.Id = entity != null ? entity.Id : throw new Exception("Invalid account returned.");
            if (entity.Attributes.ContainsKey("illumina_xeroid"))
                this.XeroId = (string)entity.Attributes["illumina_xeroid"];
            if (entity.Attributes.ContainsKey("parentaccountid"))
                this.ParentAccount = (EntityReference)entity.Attributes["parentaccountid"];
            if (entity.Attributes.ContainsKey("name"))
                this.Name = (string)entity.Attributes["name"];
            if (entity.Attributes.ContainsKey("emailaddress1"))
                this.EmailAddress = (string)entity.Attributes["emailaddress1"];
            if (entity.Attributes.ContainsKey("address1_line1"))
                this.AddressLine1 = (string)entity.Attributes["address1_line1"];
            if (entity.Attributes.ContainsKey("address1_line2"))
                this.AddressLine2 = (string)entity.Attributes["address1_line2"];
            if (entity.Attributes.ContainsKey("address1_line3"))
                this.AddressLine3 = (string)entity.Attributes["address1_line3"];
            if (entity.Attributes.ContainsKey("address1_city"))
                this.City = (string)entity.Attributes["address1_city"];
            if (entity.Attributes.ContainsKey("address1_stateorprovince"))
                this.Region = (string)entity.Attributes["address1_stateorprovince"];
            if (entity.Attributes.ContainsKey("address1_postalcode"))
                this.PostalCode = (string)entity.Attributes["address1_postalcode"];
            if (entity.Attributes.ContainsKey("address1_country"))
                this.Country = (string)entity.Attributes["address1_country"];
            if (entity.Attributes.ContainsKey("telephone1"))
                this.PhoneNumber = (string)entity.Attributes["telephone1"];
            if (entity.Attributes.ContainsKey("illumina_abn"))
                this.ABN = (string)entity.Attributes["illumina_abn"];
            if (entity.Attributes.ContainsKey("illumina_acn"))
                this.ACN = (string)entity.Attributes["illumina_acn"];
            if (entity.Attributes.ContainsKey("illumina_bankaccountname"))
                this.BankAccountName = (string)entity.Attributes["illumina_bankaccountname"];
            if (entity.Attributes.ContainsKey("illumina_bsbnumber"))
                this.BSBNumber = (string)entity.Attributes["illumina_bsbnumber"];
            if (entity.Attributes.ContainsKey("illumina_bankaccountnumber"))
                this.BankAccountNumber = (string)entity.Attributes["illumina_bankaccountnumber"];
            if (!entity.Attributes.ContainsKey("illumina_reference"))
                return;
            this.Reference = (string)entity.Attributes["illumina_reference"];
        }
    }
}
