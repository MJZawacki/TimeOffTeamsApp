using BotAuth.AADv1;
using BotAuth.Dialogs;
using BotAuth.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class TimeOffDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.SayAsync("Please give a short description of your time-off request");
            context.Wait(AfterTitleSelectedAsync);
        }

        private async Task AfterTitleSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("title", message.GetTextWithoutMentions());
            await context.SayAsync("Now enter the reason for your request");
            context.Wait(AfterCommentsSelectedAsync);
        }

        private async Task AfterCommentsSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("comments", message.GetTextWithoutMentions());

            await context.SayAsync("How many days?");
            context.Wait(AfterDaysSelectedAsync);
        }


        private async Task AfterDaysSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("days", message.GetTextWithoutMentions());

            await CompleteApprovalAsync(context, item);

        }

        private async Task CompleteApprovalAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            // Initialize AuthenticationOptions and forward to AuthDialog for token
            AuthenticationOptions options = new AuthenticationOptions()
            {
                Authority = ConfigurationManager.AppSettings["aad:Authority"],
                ClientId = ConfigurationManager.AppSettings["aad:ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["aad:ClientSecret"],
                ResourceId = ConfigurationManager.AppSettings["aad:ResourceID"],
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"]
            };
            await context.Forward(new AuthDialog(new ADALAuthProvider(), options), ResumeAfterAuthenticated, message, CancellationToken.None);
        }

        private async Task ResumeAfterAuthenticated(IDialogContext context, IAwaitable<AuthResult> authResult)
        {
            var result = await authResult;

            if (string.IsNullOrEmpty(result.AccessToken))
            {
                await context.SayAsync("Something went wrong");
                await this.EndDialog(context, null);
            }
            else
            {
                await context.SayAsync("I am now adding your request for Approval");

                var fields = new Fields()
                {
                    Title = context.ConversationData.GetValue<string>("title"),
                    Comments = context.ConversationData.GetValue<string>("comments"),
                    Days = int.Parse(context.ConversationData.GetValue<string>("days"))
                };

                var data = JsonConvert.SerializeObject(fields);

                string requestUrl = $"https://localhost:44308/api/values"; // web api endpoint
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                HttpResponseMessage response = await client.PostAsync(requestUrl, new StringContent(data, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode == true)
                {
                    await context.SayAsync("Your request has been added and is now waiting for approval.");
                }
                else
                {
                    await context.SayAsync("I could not add your request");
                }

                context.ConversationData.Clear();
                await this.EndDialog(context, null);
            }
        }

        public async Task EndDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
        }
    }
}
