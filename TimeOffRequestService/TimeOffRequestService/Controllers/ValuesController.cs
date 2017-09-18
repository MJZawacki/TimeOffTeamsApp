using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using TimeOffRequestService.Utils;
using Newtonsoft.Json;
using System.Text;
using System.Configuration;
using TimeOffRequestService.AuthorizationFilters;

namespace TimeOffRequestService.Controllers
{
    [MTAuthorize]
    public class ValuesController : ApiController
    {
        // POST api/values
        public async Task<HttpResponseMessage> Post([FromBody]Fields fields)
        {
            var helper = new AuthHelper();
            var tenant = ConfigurationManager.AppSettings["aad:Tenant"];
            var token = await helper.GetAccessToken("https://graph.microsoft.com", tenant);

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Add Sharepoint
            var requestUrl = "https://graph.microsoft.com/beta/sites/moggyinc.sharepoint.com,51e991b0-a809-451f-888e-c265c7e51a40,266e20c2-5f73-4fc2-8fd0-e977a778f303/lists/d428cb07-026a-45b7-adfb-143847c41c17/items";

            var TimeOff = new TimeOff() { fields = fields };

            string data = JsonConvert.SerializeObject(TimeOff);

            var response = await client.PostAsync(requestUrl, new StringContent(data, Encoding.UTF8, "application/json"));

            return response;
        }
    }

    public class TimeOff
    {
        public Fields fields { get; set; }
    }

    public class Fields
    {
        public string Title { get; set; }
        public string Comments { get; set; }
        public int Days { get; set; }
    }
}
