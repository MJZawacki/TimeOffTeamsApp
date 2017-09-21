﻿using BotAuth.AADv1;
using BotAuth.Dialogs;
using BotAuth.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
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
using TimeOffBot.DAL;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class TimeOffDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await Task.Run(() =>
            {
                context.SayAsync("Please give a short description of your time-off request");
                context.Wait(MessageReceivedAsync);
            });
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item;

            // store the conversation.Id for later use when informing the user their request is approved
            context.ConversationData.SetValue<string>("conversationId", message.Conversation.Id);
            
            context.ConversationData.SetValue<string>("title", message.GetTextWithoutMentions());
            await context.SayAsync("Are there any special circumstances the Approver needs to be aware of?");
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
            await context.Forward(new AuthDialog(new ADALAuthProvider(), options), ResumeAfterAuthenticated, 
                message, CancellationToken.None);
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
                    Days = int.Parse(context.ConversationData.GetValue<string>("days")),
                    ConversationId = context.ConversationData.GetValue<string>("conversationId")
                };

                var data = JsonConvert.SerializeObject(fields);

                string requestUrl = $"https://localhost:44308/api/values"; // web api endpoint
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                HttpResponseMessage response = await client.PostAsync(requestUrl, new StringContent(data, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode == true)
                {
                    var UserService = new UserManagerService(false);

                    // let the user know their request is awaiting approval
                    var message = context.MakeMessage();
                    var attachment = GetThumbnailCard(context.ConversationData.GetValue<string>("title"));
                    message.Attachments.Add(attachment);
                    await context.PostAsync((message));

                    TeamsChannelData currentChannelData = context.Activity.GetChannelData<TeamsChannelData>();
                    string tenantId = currentChannelData.Tenant.Id;
                    var botdata = UserService.GetBotRegistration(tenantId);

                    // save the result to DocumentDB so that the message can be updated once approved or rejected
                    var conversation = new ConversationData()
                    {
                        toId = message.From.Id,
                        toName = message.From.Name,
                        fromId = message.Recipient.Id,
                        fromName = message.Recipient.Name,
                        serviceUrl = message.ServiceUrl,
                        channelId = message.ChannelId,
                        conversationId = message.Conversation.Id,
                        teamId = botdata.teamId,
                        originatingMessage = message.Id,
                        tenantId = tenantId
                    };

                    
                    await UserService.SaveConversation(conversation);
                }
                else
                {
                    await context.SayAsync("I could not add your request");
                }

                context.ConversationData.Clear();
                await this.EndDialog(context, null);
            }
        }

        private static Attachment GetThumbnailCard(string text)
        {
            var heroCard = new ThumbnailCard
            {
                Title = "Time-off Request",
                Subtitle = "Request Awaiting Approval",
                Text = text,
                Images = new List<CardImage> { new CardImage("http://freedesignfile.com/upload/2014/04/Summer-beach-vacation-background-art-vector-01.jpg") }
            };

            return heroCard.ToAttachment();
        }

        public async Task EndDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
        }
    }
}
