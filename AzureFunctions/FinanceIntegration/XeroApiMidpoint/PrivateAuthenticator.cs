// Decompiled with JetBrains decompiler
// Type: XeroApiMidpoint.PrivateAuthenticator
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using System;
using System.Security.Cryptography.X509Certificates;
using Xero.Api.Infrastructure.Interfaces;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Infrastructure.OAuth.Signing;

namespace XeroApiMidpoint
{
    public class PrivateAuthenticator : IAuthenticator
    {
        private readonly X509Certificate2 _certificate;
        private X509KeyStorageFlags keyStorageFlags;

        public PrivateAuthenticator(string certificatePath)
        {
            this._certificate = new X509Certificate2();
            this._certificate.Import(certificatePath, "", this.keyStorageFlags);
        }

        public PrivateAuthenticator(X509Certificate2 certificate) => this._certificate = certificate;

        public string GetSignature(
          IConsumer consumer,
          IUser user,
          Uri uri,
          string verb,
          IConsumer consumer1)
        {
            RsaSha1Signer rsaSha1Signer = new RsaSha1Signer();
            X509Certificate2 certificate = this._certificate;
            Token token = new Token();
            token.ConsumerKey = consumer.ConsumerKey;
            token.ConsumerSecret = consumer.ConsumerSecret;
            token.TokenKey = consumer.ConsumerKey;
            Uri uri1 = uri;
            string verb1 = verb;
            var test = rsaSha1Signer.CreateSignature(certificate, (IToken)token, uri1, verb1);
            return rsaSha1Signer.CreateSignature(certificate, (IToken)token, uri1, verb1);


        }

        public X509Certificate Certificate => (X509Certificate)this._certificate;

        public IToken GetToken(IConsumer consumer, IUser user) => (IToken)null;

        public IUser User { get; set; }
    }
}
