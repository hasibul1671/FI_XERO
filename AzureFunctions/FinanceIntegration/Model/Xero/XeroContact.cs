// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Xero.XeroContact
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Data.OData.Query.SemanticAst;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Xero.Api.Core;
using Xero.Api.Core.Model;
using Xero.Api.Core.Model.Types;
using Xero.Api.Core.Response;
using Xero.Api.Example.Applications.Private;
using Xero.Api.Infrastructure.Interfaces;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Serialization;

namespace FinanceIntegration.Model.Xero
{
    public static class XeroContact
    {
        public static Contact buildContact(FinanceIntegration.Model.Dynamics.Account supplier, TraceWriter log)
        {
            log.Info("Entered buildContact - Build JSON for API call.");
            Contact contact = new Contact();
            Phone phone = new Phone();
            Address address = new Address();
            phone.PhoneNumber = supplier.PhoneNumber;
            address.AddressLine1 = supplier.AddressLine1;
            address.AddressLine2 = supplier.AddressLine2;
            address.AddressLine3 = supplier.AddressLine3;
            address.City = supplier.City;
            address.Region = supplier.Region;
            address.PostalCode = supplier.PostalCode;
            address.Country = supplier.Country;
            address.AddressType = AddressType.PostOfficeBox;
            contact.Name = supplier.Name;
            contact.EmailAddress = supplier.EmailAddress;
            contact.ContactNumber = supplier.Id.ToString();
            contact.TaxNumber = supplier.ABN;
            BatchPayments batchPayments = new BatchPayments();
            batchPayments.BankAccountName = supplier.BankAccountName;
            batchPayments.BankAccountNumber = supplier.BankAccountNumber;
            batchPayments.Details = supplier.Reference;
            log.Info("Perform Account check");
            if (supplier.XeroId == string.Empty)
            {
                //X509Store x509Store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                //x509Store.Open(OpenFlags.ReadOnly);
                //X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, (object)Configuration.CertKey, false);
                //X509Certificate2 certificate = (X509Certificate2)null;
                //if (certificate2Collection.Count > 0)
                //    certificate = certificate2Collection[0];
                //else
                //    log.Info("No Cert Found - We got some issues.");
                //x509Store.Close();
                //XeroCoreApi xeroCoreApi = new XeroCoreApi("https://api.xero.com", (IAuthenticator)new PrivateAuthenticator(certificate), (IConsumer)new Consumer(Configuration.ConsumerKey, Configuration.ConsumerSecret), (IUser)null, (IJsonObjectMapper)new DefaultMapper(), (IXmlObjectMapper)new DefaultMapper());

                string accessToken = "";
                string tenantId = null;

                string retrievedAccessToken = XeroClient.getAccessToken(); // Will generate a new token and save it to the file
                if (retrievedAccessToken == null)
                {
                    log.Info("The Access Token was not retrieved properly.");
                }
                else
                {
                    accessToken = retrievedAccessToken;
                }

                tenantId = XeroClient.getOrganization(accessToken);
                if (tenantId == null)
                {
                    log.Info("XeroClient : Error getting organization ID needed to post payloads as per Xero Api guide");
                }

                try
                {
                    log.Info("Try via Contact Number");
                    //List<Contact> source = (List<Contact>)xeroCoreApi.Contacts.Where("ContactNumber==\"" + supplier.Id.ToString() + "\"").Find();
                    var contactListPayload = XeroClient.getContacts(accessToken, tenantId);
                    List<Contact> source = new List<Contact>();

                    if (contactListPayload.Contacts.Count() != 0)
                    {
                        var contactFound = contactListPayload.Contacts.FirstOrDefault(contactItem => contactItem.ContactNumber == supplier.Id.ToString());

                        if (contactFound != null)
                        {
                            log.Info("Found via Contact Number");
                            XeroContact.updateAccount(supplier.Id, Guid.Parse(contactFound.ContactID), log);
                        }
                        else
                        {
                            contactFound = contactListPayload.Contacts.FirstOrDefault(contactItem => contactItem.Name.ToLower() == supplier.Name.ToLower());
                            if (contactFound != null)
                            {
                                log.Info("Found via Contact Number");
                                XeroContact.updateAccount(supplier.Id, Guid.Parse(contactFound.ContactID), log);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
                contact.Id = Guid.Parse(supplier.XeroId);
                List<Phone> phoneList = new List<Phone>();
                List<Address> addressList = new List<Address>();
                phoneList.Add(phone);
                addressList.Add(address);
                contact.Phones = phoneList;
                contact.Addresses = addressList;
                contact.BatchPayments = batchPayments;
            }

            return contact;

        }

        public static void updateAccount(Guid account, Guid xeroContact, TraceWriter log)
        {
            log.Info("Entered updateAccount - Updating CRM Account with Xero Id");
            new WebProxyClient().Update(new Entity(nameof(account), account)
            {
                ["illumina_xeroid"] = (object)xeroContact.ToString()
            });
            log.Info("Exiting updateAccount");
        }
    }
}
