// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.WebProxyClient
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk.WebServiceClient;
using System;
using System.Reflection;

namespace FinanceIntegration.Model
{
    public class WebProxyClient : OrganizationWebProxyClient
    {
        private AuthenticationContext _authCtx;
        private string _authResource = string.Empty;

        public WebProxyClient()
          : base(new Uri(Configuration.D365OrgSvcUrl), false)
        {
            this.RefreshToken();
        }

        public void RefreshToken()
        {
            if (this._authCtx == null)
            {
                lock (this._authResource)
                {
                    AuthenticationParameters result = AuthenticationParameters.CreateFromResourceUrlAsync(new Uri(Configuration.D365OrgSvcUrl)).Result;
                    this._authCtx = new AuthenticationContext(result.Authority);
                    this._authResource = result.Resource;
                }
            }
            if (this._authCtx == null || string.IsNullOrWhiteSpace(this._authResource))
                throw new Exception("Unable to initialise AAD authentiation context");
            AuthenticationResult result1 = this._authCtx.AcquireTokenAsync(this._authResource, new ClientCredential(Configuration.AadAppId, Configuration.AadAppkey)).Result;
            if (result1 == null || result1.AccessToken.Length <= 0)
                throw new Exception("Unable to acquire token");
            this.HeaderToken = result1.AccessToken;
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
