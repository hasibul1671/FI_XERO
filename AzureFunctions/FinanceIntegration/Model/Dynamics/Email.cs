// Decompiled with JetBrains decompiler
// Type: FinanceIntegration.Model.Dynamics.Email
// Assembly: FinanceIntegration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 6E44CBBF-3F39-4AB4-BCB1-EE9254BB59D6
// Assembly location: C:\Users\Illuminance\Downloads\FinanceIntegration\bin\FinanceIntegration.dll

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace FinanceIntegration.Model.Dynamics
{
    public static class Email
    {
        public static void SendEmail(
          Guid toUser,
          Guid fromUser,
          EntityReference regarding,
          string subject,
          string body)
        {
            WebProxyClient webProxyClient = new WebProxyClient();
            Entity entity1 = webProxyClient.Retrieve("systemuser", toUser, new ColumnSet(new string[2]
            {
        "internalemailaddress",
        "fullname"
            }));
            Entity entity2 = webProxyClient.Retrieve("systemuser", fromUser, new ColumnSet(new string[2]
            {
        "internalemailaddress",
        "fullname"
            }));
            Entity entity3 = new Entity("activityparty");
            entity3["addressused"] = (object)entity1.GetAttributeValue<string>("internalemailaddress");
            entity3["partyid"] = (object)entity1.ToEntityReference();
            Entity entity4 = new Entity("activityparty");
            entity4["addressused"] = (object)entity2.GetAttributeValue<string>("internalemailaddress");
            entity4["partyid"] = (object)entity2.ToEntityReference();
            EntityCollection entityCollection1 = new EntityCollection();
            entityCollection1.Entities.Add(entity3);
            EntityCollection entityCollection2 = new EntityCollection();
            entityCollection2.Entities.Add(entity4);
            Guid guid = webProxyClient.Create(new Entity("email")
            {
                [nameof(subject)] = (object)subject,
                ["to"] = (object)entityCollection1,
                ["from"] = (object)entityCollection2,
                ["ownerid"] = (object)entity2.ToEntityReference(),
                ["regardingobjectid"] = (object)regarding,
                ["description"] = (object)body
            });
            webProxyClient.CallerId = toUser;
            SendEmailRequest request = new SendEmailRequest()
            {
                EmailId = guid,
                TrackingToken = "",
                IssueSend = true
            };
            SendEmailResponse sendEmailResponse = (SendEmailResponse)webProxyClient.Execute((OrganizationRequest)request);
        }
    }
}
