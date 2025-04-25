using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk.WebServiceClient;
using System;
using System.Reflection;

namespace FinanceIntegration.Model
{
    public class WebProxyClient : OrganizationWebProxyClient
    {
        private AuthenticationContext _authCtx;

        public WebProxyClient()
          : base(new Uri("https://yacatmsqa.crm6.dynamics.com/xrmservices/2011/organization.svc/web?SdkClientVersion=8.2"), false)
        {
            this.RefreshToken();
        }

        public void RefreshToken()
        {
            // Replace with your actual tenant ID
            //string authority = "https://login.microsoftonline.com/<your-tenant-id>";
            string authority = "https://login.microsoftonline.com/1b8e11fa-d1eb-424f-833e-631ee6acda36";
            string resource = "https://yacatmsqa.api.crm6.dynamics.com";

            _authCtx = new AuthenticationContext(authority);
            var credential = new ClientCredential(
                "d3072029-bef8-4f8a-ac0e-b5e7f84508ec",
                "IFQv0EjuykQhUAfas0PvtrW25PEAk5kuAjbX7a9gwhM=");

            AuthenticationResult result = _authCtx.AcquireTokenAsync(resource, credential).Result;

            if (result == null || string.IsNullOrEmpty(result.AccessToken))
                throw new Exception("Unable to acquire token");

            this.HeaderToken = result.AccessToken;
        }

        public WebProxyClient(Uri serviceUrl, Assembly strongTypeAssembly)
          : base(serviceUrl, strongTypeAssembly)
        {
            throw new NotImplementedException("Stop it");
        }

        public WebProxyClient(Uri serviceUrl, TimeSpan timeout, bool useStrongTypes)
          : base(serviceUrl, timeout, useStrongTypes)
        {
            throw new NotImplementedException("Stop it");
        }

        public WebProxyClient(Uri uri, TimeSpan timeout, Assembly strongTypeAssembly)
          : base(uri, timeout, strongTypeAssembly)
        {
            throw new NotImplementedException("Stop it");
        }

        public WebProxyClient(Uri serviceUrl, bool useStrongTypes)
          : base(serviceUrl, useStrongTypes)
        {
            throw new NotImplementedException("Stop it");
        }
    }
}
