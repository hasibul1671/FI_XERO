// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Configuration
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;

using Xero.Api.Core.Model;

namespace FinanceIntegration.Model
{
    public class Configuration 
    {
        private static string _sbConnectionString = string.Empty;
        private static string _aadAppId = string.Empty;
        private static string _aadAppKey = string.Empty;
        private static string _d365ODataUrl = string.Empty;
        private static string _d365OrgSvcUrl = string.Empty;
        private static string _publicPrivateCert = string.Empty;
        private static string _consumerKey = string.Empty;
        private static string _consumerSecret = string.Empty;
        private static string _documentTemplateName = string.Empty;
        private static string _senderQueueId = string.Empty;
        private static string _requiredTemplateId = string.Empty;
        private static string _notRequiredTemplateId = string.Empty;
        private static string _pdfConverterUri = string.Empty;
        private static string _xeroShortCode = string.Empty;
        private static string _xeroHookKey = string.Empty;
        private static string _certKey = string.Empty;
        private static string _stringConnection = string.Empty;
        private static string _xero_client_Id = string.Empty;
        private static string _xero_client_secret = string.Empty;

        public static string SbConnectionString
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._sbConnectionString))
                {
                    lock (FinanceIntegration.Model.Configuration._sbConnectionString)
                        FinanceIntegration.Model.Configuration._sbConnectionString = ConfigurationManager.AppSettings["sbConnectionString"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._sbConnectionString) ? FinanceIntegration.Model.Configuration._sbConnectionString : throw new Exception("Unable to read Azure Service Bus connection string");
            }
        }

        public static string XeroClientId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xero_client_Id))
                {
                    lock (FinanceIntegration.Model.Configuration._xero_client_Id)
                        FinanceIntegration.Model.Configuration._xero_client_Id = ConfigurationManager.AppSettings["xero_client_id"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xero_client_Id) ? FinanceIntegration.Model.Configuration._xero_client_Id : throw new Exception("Unable to read xero_client_id");
            }

        }  public static string XeroClientSecret
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xero_client_secret))
                {
                    lock (FinanceIntegration.Model.Configuration._xero_client_secret)
                        FinanceIntegration.Model.Configuration._xero_client_secret = ConfigurationManager.AppSettings["xero_client_secret"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xero_client_secret) ? FinanceIntegration.Model.Configuration._xero_client_secret : throw new Exception("Unable to read xero_client_secret");
            }
        }

        public static string AadAppId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._aadAppId))
                {
                    lock (FinanceIntegration.Model.Configuration._aadAppId)
                        FinanceIntegration.Model.Configuration._aadAppId = ConfigurationManager.AppSettings["aadAppId"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._aadAppId) ? FinanceIntegration.Model.Configuration._aadAppId : throw new Exception("Unable to read Azure AD SMS application id");
            }
        }

        public static string AadAppkey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._aadAppKey))
                {
                    lock (FinanceIntegration.Model.Configuration._aadAppKey)
                        FinanceIntegration.Model.Configuration._aadAppKey = ConfigurationManager.AppSettings["aadAppKey"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._aadAppKey) ? FinanceIntegration.Model.Configuration._aadAppKey : throw new Exception("Unable to read Azure AD SMS key");
            }
        }

        public static string D365ODataUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._d365ODataUrl))
                {
                    lock (FinanceIntegration.Model.Configuration._d365ODataUrl)
                        FinanceIntegration.Model.Configuration._d365ODataUrl = ConfigurationManager.AppSettings["d365ODataUrl"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._d365ODataUrl) ? FinanceIntegration.Model.Configuration._d365ODataUrl : throw new Exception("Unable to read Dynamics 365 OData URL");
            }
        }

        public static string D365OrgSvcUrl
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._d365OrgSvcUrl))
                {
                    lock (FinanceIntegration.Model.Configuration._d365OrgSvcUrl)
                        FinanceIntegration.Model.Configuration._d365OrgSvcUrl = ConfigurationManager.AppSettings["d365OrgSvcUrl"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._d365OrgSvcUrl) ? FinanceIntegration.Model.Configuration._d365OrgSvcUrl : throw new Exception("Unable to read Dynamics 365 Organization Service URL");
            }
        }

        public static string PublicPrivateCert
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._publicPrivateCert))
                {
                    lock (FinanceIntegration.Model.Configuration._publicPrivateCert)
                        FinanceIntegration.Model.Configuration._publicPrivateCert = ConfigurationManager.AppSettings["publicPrivateCert"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._publicPrivateCert) ? FinanceIntegration.Model.Configuration._publicPrivateCert : throw new Exception("Unable to read Cert");
            }
        }

        public static string ConsumerKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._consumerKey))
                {
                    lock (FinanceIntegration.Model.Configuration._consumerKey)
                        FinanceIntegration.Model.Configuration._consumerKey = ConfigurationManager.AppSettings["consumerKey"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._consumerKey) ? FinanceIntegration.Model.Configuration._consumerKey : throw new Exception("Unable to read Con Key");
            }
        }

        public static string ConsumerSecret
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._consumerSecret))
                {
                    lock (FinanceIntegration.Model.Configuration._consumerSecret)
                        FinanceIntegration.Model.Configuration._consumerSecret = ConfigurationManager.AppSettings["consumerSecret"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._consumerSecret) ? FinanceIntegration.Model.Configuration._consumerSecret : throw new Exception("Unable to read Con Secret");
            }
        }

        public static string DocumentTemplateName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._documentTemplateName))
                {
                    lock (FinanceIntegration.Model.Configuration._documentTemplateName)
                        FinanceIntegration.Model.Configuration._documentTemplateName = ConfigurationManager.AppSettings["documentTemplateName"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._documentTemplateName) ? FinanceIntegration.Model.Configuration._documentTemplateName : throw new Exception("Unable to read document template ID");
            }
        }

        public static string SenderQueueId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._senderQueueId))
                {
                    lock (FinanceIntegration.Model.Configuration._senderQueueId)
                        FinanceIntegration.Model.Configuration._senderQueueId = ConfigurationManager.AppSettings["senderQueueId"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._senderQueueId) ? FinanceIntegration.Model.Configuration._senderQueueId : throw new Exception("Unable to read sender queue ID");
            }
        }

        public static string RequiredTemplateId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._requiredTemplateId))
                {
                    lock (FinanceIntegration.Model.Configuration._requiredTemplateId)
                        FinanceIntegration.Model.Configuration._requiredTemplateId = ConfigurationManager.AppSettings["requiredTemplateId"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._requiredTemplateId) ? FinanceIntegration.Model.Configuration._requiredTemplateId : throw new Exception("Unable to read tax invoice required template ID");
            }
        }

        public static string NotRequiredTemplateId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._notRequiredTemplateId))
                {
                    lock (FinanceIntegration.Model.Configuration._notRequiredTemplateId)
                        FinanceIntegration.Model.Configuration._notRequiredTemplateId = ConfigurationManager.AppSettings["notRequiredTemplateId"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._notRequiredTemplateId) ? FinanceIntegration.Model.Configuration._notRequiredTemplateId : throw new Exception("Unable to read tax invoice not required template ID");
            }
        }

        public static string PdfConverterUri
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._pdfConverterUri))
                {
                    lock (FinanceIntegration.Model.Configuration._pdfConverterUri)
                        FinanceIntegration.Model.Configuration._pdfConverterUri = ConfigurationManager.AppSettings["pdfConverterUri"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._pdfConverterUri) ? FinanceIntegration.Model.Configuration._pdfConverterUri : throw new Exception("Unable to read PDF converter URI");
            }
        }

        public static string XeroShortCode
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xeroShortCode))
                {
                    lock (FinanceIntegration.Model.Configuration._xeroShortCode)
                        FinanceIntegration.Model.Configuration._xeroShortCode = ConfigurationManager.AppSettings["xeroShortCode"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xeroShortCode) ? FinanceIntegration.Model.Configuration._xeroShortCode : throw new Exception("Unable to read Xero Short Code");
            }
        }

        public static string XeroHookKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xeroHookKey))
                {
                    lock (FinanceIntegration.Model.Configuration._xeroHookKey)
                        FinanceIntegration.Model.Configuration._xeroHookKey = ConfigurationManager.AppSettings["xeroHookKey"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._xeroHookKey) ? FinanceIntegration.Model.Configuration._xeroHookKey : throw new Exception("Unable to read Xero Hook Key");
            }
        }

        public static string CertKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._certKey))
                {
                    lock (FinanceIntegration.Model.Configuration._certKey)
                        FinanceIntegration.Model.Configuration._certKey = ConfigurationManager.AppSettings["WEBSITE_LOAD_CERTIFICATES"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._certKey) ? FinanceIntegration.Model.Configuration._certKey : throw new Exception("Unable to read Xero Hook Key");
            }
        }


        public static string StorageAccountStringConnection
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._stringConnection))
                {
                    lock (FinanceIntegration.Model.Configuration._stringConnection)
                        FinanceIntegration.Model.Configuration._stringConnection = ConfigurationManager.AppSettings["AzureWebJobsStorage"] ?? string.Empty;
                }
                return !string.IsNullOrWhiteSpace(FinanceIntegration.Model.Configuration._stringConnection) ? FinanceIntegration.Model.Configuration._stringConnection : throw new Exception("Unable to read Azure Web Jobs Storage in FinanceConfiguaration Table");
            }
        }

      
    }
}
