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
using TimeOffBot.Utils;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class TimeOffDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.SayAsync("Please give a short description of your time-off request");
            context.Wait(MessageReceivedAsync);
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
                RedirectUrl = ConfigurationManager.AppSettings["aad:Callback"],
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
                IMessageActivity message = context.MakeMessage();
                var card = CardHelper.MakeRequestCard(context.ConversationData.GetValue<string>("title"), "Posting Request for Approval");
                message.Attachments.Add(card.ToAttachment());

                ConnectorClient connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));
                ResourceResponse resp = await connector.Conversations.ReplyToActivityAsync((Activity)message);

                // Cache the response activity ID and previous task card.
                string activityId = resp.Id.ToString();
                context.ConversationData.SetValue("requestId", activityId);

                var fields = new Fields()
                {
                    Title = context.ConversationData.GetValue<string>("title"),
                    Comments = context.ConversationData.GetValue<string>("comments"),
                    Days = int.Parse(context.ConversationData.GetValue<string>("days")),
                    ConversationId = context.ConversationData.GetValue<string>("conversationId"),
                    MessageId = activityId
                };

                var data = JsonConvert.SerializeObject(fields);

                string requestUrl = $"https://localhost:44308/api/values"; // web api endpoint
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);

                HttpResponseMessage response = await client.PostAsync(requestUrl, new StringContent(data, Encoding.UTF8, "application/json"));

                string existingActivityId = string.Empty;

                if (response.IsSuccessStatusCode == true)
                {

                    var UserService = new UserManagerService(false);
                    // let the admin know there is a request to approve
                    TeamsChannelData currentChannelData = context.Activity.GetChannelData<TeamsChannelData>();
                    string tenantId = currentChannelData.Tenant.Id;
                    var approvalChannelId = UserService.GetApprovalChannel(tenantId)?.channelId;
                    // Create a new message.
                    var channelData = new TeamsChannelData { Channel = new ChannelInfo(approvalChannelId) };
                    IMessageActivity newMessage = Activity.CreateMessageActivity();
                    var heroCard = CardHelper.MakeRequestCard(context.Activity.From.Name + " sent a time off request", "Please approve this request");

                    newMessage.Type = ActivityTypes.Message;
                    //newMessage.Text = context.Activity.From.Name + " sent a time off request";
                    newMessage.Attachments.Add(heroCard.ToAttachment());
                    //newMessage.Text = "Hello, on a new thread";
                    ConversationParameters conversationParams = new ConversationParameters(
                        isGroup: true,
                        bot: null,
                        members: null,
                        topicName: "Test Conversation",
                        activity: (Activity)newMessage,
                        channelData: channelData);
                    var approverNotifyResult = await connector.Conversations.CreateConversationAsync(conversationParams);

                    // let the user know their request is awaiting approval

                   

                    if (context.ConversationData.TryGetValue("requestId", out existingActivityId))

                    {
                        IMessageActivity reply = context.MakeMessage();

                        var newCard = CardHelper.MakeRequestCard(context.ConversationData.GetValue<string>("title"), "Request Pending Approval");

                        reply.Attachments.Add(newCard.ToAttachment());
                        ResourceResponse resourceResponse = await connector.Conversations.UpdateActivityAsync(context.Activity.Conversation.Id, existingActivityId, (Activity)reply);
                    }

                 
                    // save the result to DocumentDB so that the message can be updated once approved or rejected
                    var conversation = new DAL.ConversationData();
                    conversation.toId = message.From.Id;
                    conversation.toName = message.From.Name;
                    conversation.fromId = message.Recipient.Id;
                    conversation.fromName = message.Recipient.Name;
                    conversation.serviceUrl = message.ServiceUrl;
                    conversation.channelId = message.ChannelId;
                    conversation.conversationId = message.Conversation.Id;
                    conversation.originatingMessage = existingActivityId;
              
                    await UserService.SaveConversation(conversation);
                }
                else
                {
                    if (context.ConversationData.TryGetValue("requestId", out existingActivityId))
                    {
                        IMessageActivity reply = context.MakeMessage();

                        var newCard = CardHelper.MakeRequestCard(context.ConversationData.GetValue<string>("title"), "Request not added, please see your IT admin");

                        reply.Attachments.Add(newCard.ToAttachment());
                        ResourceResponse resourceResponse = await connector.Conversations.UpdateActivityAsync(context.Activity.Conversation.Id, existingActivityId, (Activity)reply);
                    }
                    else
                    {
                        await context.SayAsync("Request not added, please see your IT admin");
                    }
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
