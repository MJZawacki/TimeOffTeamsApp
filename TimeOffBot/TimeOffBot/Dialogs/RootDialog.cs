using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Configuration;
using BotAuth.Models;
using BotAuth.Dialogs;
using BotAuth.AADv1;
using BotAuth;
using System.Threading;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace TeamsBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await Task.Run(() =>
            {
                context.Wait(MessageReceivedAsync);
            });
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            await Task.Run(() =>
            {
                var descriptions = new List<string>() { "Time Off", "Sickness", "Business Travel" };
                var choices = new Dictionary<string, IReadOnlyList<string>>()
                {
                        { "Time Off", new List<string> { "Time Off" } },
                        { "Sickness", new List<string> { "Sick" } },
                        { "Business Travel", new List<string> { "Travel"} },
                };

                var promptOptions = new PromptOptionsWithSynonyms<string>(
                    $"Hey, what requests do you want to make today?",
                    "I am sorry but I didn't understand that. I need you to select one of the options below",
                    choices: choices,
                    descriptions: descriptions,
                    speak: $"Hey, what requests do you want to make today?",
                    retrySpeak: "I am sorry but I didn't get that. Please say Time Off, Sickness or Business Travel",
                    attempts: 2);

                PromptDialog.Choice(
                    context,
                    this.AfterChoiceSelected,
                    promptOptions);
            });
        }

        private async Task AfterChoiceSelected(IDialogContext context, IAwaitable<string> result)
        {
            var activity = context.Activity as Activity;

            try
            {
                var selection = await result;

                switch (selection)
                {
                    case "Sickness":
                        await context.PostAsync("This feature is not yet implemented!");
                        await this.StartAsync(context);
                        break;

                    case "Time Off":
                        {
                            await GetRequest(context);
                        }
                        break;

                    case "Business Travel":
                        await context.PostAsync("This feature is not yet implemented!");
                        await this.StartAsync(context);
                        break;
                }
            }
            catch (TooManyAttemptsException)
            {
                await this.StartAsync(context);
            }
        }

        private async Task GetRequest(IDialogContext context)
        {
            await context.SayAsync("Please give a short description of your request");
            context.Wait(AfterTitleSelectedAsync);
        }

        public virtual async Task AfterTitleSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("title", message.Text);
            await context.SayAsync("Now enter the reason for your request");
            context.Wait(AfterCommentsSelectedAsync);
        }

        private async Task AfterCommentsSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("comments", message.Text);

            await context.SayAsync("How many days?");
            context.Wait(AfterDaysSelectedAsync);
        }


        private async Task AfterDaysSelectedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            context.ConversationData.SetValue<string>("days", message.Text);

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
                context.Wait(MessageReceivedAsync);
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
                } else
                {
                    await context.SayAsync("I could not add your request");
                }

                context.ConversationData.Clear();
                await this.StartAsync(context);
            }
        }
    }

    public class Fields
    {
        public string Title { get; set; }
        public string Comments { get; set; }
        public int Days { get; set; }
    }
}

