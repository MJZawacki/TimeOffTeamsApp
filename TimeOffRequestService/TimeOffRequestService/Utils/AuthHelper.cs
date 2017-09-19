using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TimeOffRequestService.Utils
{
    public class AuthHelper
    {
        private string clientId = ConfigurationManager.AppSettings["ida:ClientID"];
        private string clientSecret = ConfigurationManager.AppSettings["ida:Password"];

        public async Task<string> GetAccessToken (string resource)
        {
            ClientCredential clientCred = new ClientCredential(clientId, clientSecret);
            string authHeader = HttpContext.Current.Request.Headers["Authorization"];

            string userAccessToken = authHeader.Substring(authHeader.LastIndexOf(' ')).Trim();
            UserAssertion userAssertion = new UserAssertion(userAccessToken);

            string authority = $"https://login.microsoftonline.com/common";

            AuthenticationContext authContext = new AuthenticationContext(authority);

            var result = await authContext.AcquireTokenAsync(resource, clientCred, userAssertion);

            return result.AccessToken;
        }
    }
}