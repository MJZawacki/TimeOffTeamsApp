using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using TimeOffBot.Utils;
using TimeOffBot.DAL;
using Microsoft.Bot.Connector.Teams.Models;
using System.Configuration;

namespace TimeOffBot.Controllers
{
    public class NotificationsController : ApiController
    {
        private static IUserManagerService _userService;
        public NotificationsController()
        {
            var debugflag = ConfigurationManager.AppSettings["debugFlag"];
            _userService = new UserManagerService(Boolean.Parse(debugflag));
        }
        [HttpPost]

        [Route("api/notifications")]
        public async Task<HttpResponseMessage> SendMessage([FromBody]MessagePostDAO messagedata)
        {
           

            var type = messagedata.msgType;
            if (type == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Message type is missing from body");
            }
            var conversationData = _userService.GetConversation(messagedata.conversationID);
            switch (type) {
                case "newTimeOffRequest":
                    // get approvers channel

                    // send message

                    break;
                case "timeOffResponse":
                    // Update original request in the channel
                    try
                        {
                            return await sendTimeOffResponse(messagedata, conversationData);
                        }            
                    catch (Exception ex)
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
                        }
                case "toChannel":
                    // Send whatever message was included in the POST message
                    //await sendCard(messagedata.responseMsg, conversationData);
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>Message sent, thanks.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                case "notifyApprover":
                    // Send whatever message was included in the POST message
                    await sendApproverRequestMsg(messagedata.responseMsg, conversationData);
                    resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>Message sent, thanks.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;

            }
            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Error");

        }

        private static async Task sendApproverRequestMsg(string textmessage, ConversationData data)
        {
            // lookup approvers channelid

            var approvalChannelId = _userService.GetApprovalChannel(data.tenantId)?.channelId;
            var userAccount = new ChannelAccount(data.toId, data.toName);
            var botAccount = new ChannelAccount(data.fromId, data.fromName);
            var connector = new ConnectorClient(new Uri(data.serviceUrl));

            // Create a new message.

            var heroCard = new ThumbnailCard
            {
                Title = "Time-off Request",
                Subtitle = data.fromName + "Sent a time off request",
                Text = "Please approve this request",
                Images = new List<CardImage> { new CardImage("http://freedesignfile.com/upload/2014/04/Summer-beach-vacation-background-art-vector-01.jpg") }
            };


            IMessageActivity newMessage = Activity.CreateMessageActivity();
            newMessage.Type = ActivityTypes.Message;
            newMessage.Attachments.Add(heroCard.ToAttachment());
            //newMessage.From = botAccount;
            newMessage.Text = textmessage;
            newMessage.Locale = "en-us";

            if (!string.IsNullOrEmpty(data.conversationId) && !string.IsNullOrEmpty(data.channelId))
            {
                // If conversation ID and channel ID was stored previously, use it.
                newMessage.ChannelId = data.channelId;
            }
            else
            {
                // Conversation ID was not stored previously, so create a conversation. 
                // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                data.conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
            }

            var channelData = new TeamsChannelData { Channel = new ChannelInfo(approvalChannelId) };

            ConversationParameters conversationParams = new ConversationParameters(
                isGroup: true,
                bot: null,
                members: null,
                topicName: "Test Conversation",
                activity: (Activity)newMessage,
                channelData: channelData);
            var result = await connector.Conversations.CreateConversationAsync(conversationParams);
        }
        private static async Task sendNewChannelMsg(string textmessage, ConversationData data)
        {
            // lookup approvers channelid

            var approvalChannelId = _userService.GetApprovalChannel(data.tenantId)?.channelId;
            var userAccount = new ChannelAccount(data.toId, data.toName);
            var botAccount = new ChannelAccount(data.fromId, data.fromName);
            var connector = new ConnectorClient(new Uri(data.serviceUrl));

            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();
            message.Type = ActivityTypes.Message;
            message.Text = textmessage;
            message.ChannelId = approvalChannelId;

            var channelData = new TeamsChannelData { Channel = new ChannelInfo(approvalChannelId) };
        

            ConversationParameters conversationParams = new ConversationParameters(
                isGroup: true,
                bot: null,
                members: null,
                topicName: "Test Conversation",
                activity: (Activity) message,
                channelData: channelData);
            var result = await connector.Conversations.CreateConversationAsync(conversationParams);
        }


        private static async Task sendCard(string textmessage, ConversationData data)
        {

            var userAccount = new ChannelAccount(data.toId, data.toName);
            var botAccount = new ChannelAccount(data.fromId, data.fromName);
            var connector = new ConnectorClient(new Uri(data.serviceUrl));

            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();
            if (!string.IsNullOrEmpty(data.conversationId) && !string.IsNullOrEmpty(data.channelId))
            {
                // If conversation ID and channel ID was stored previously, use it.
                message.ChannelId = data.channelId;
            }
            else
            {
                // Conversation ID was not stored previously, so create a conversation. 
                // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                data.conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
            }

            // Set the address-related properties in the message and send the message.
            message.From = botAccount;
            message.Recipient = userAccount;
            message.Conversation = new ConversationAccount(id: data.conversationId);
            //message.Text = textmessage;
            message.Locale = "en-us";
  

            var heroCard = new HeroCard
            {
                Title = "BotFramework Hero Card",
                Subtitle = "Your bots — wherever your users are talking",
                Text = "Build and connect intelligent bots to interact with your users naturally wherever they are, from text/sms to Skype, Slack, Office 365 mail and other popular services.",
                Images = new List<CardImage> { new CardImage("https://sec.ch9.ms/ch9/7ff5/e07cfef0-aa3b-40bb-9baa-7c9ef8ff7ff5/buildreactionbotframework_960.jpg") },
                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Get Started", value: "https://docs.microsoft.com/bot-framework") }
            };

            

            message.Attachments.Add(heroCard.ToAttachment());

            try
            {
                await connector.Conversations.SendToConversationAsync((Activity)message);
            }
            catch (Exception ex)
            {
                var thrownException = ex;
            }
            
        }
        private static async Task<HttpResponseMessage> sendTimeOffResponse(MessagePostDAO messageData, ConversationData conversationData)
        {
                if (!string.IsNullOrEmpty(conversationData?.channelId))
                {
                    await SendMessage(messageData.responseMsg, messageData.originatingMessageId, messageData.originatingRequestReason, conversationData); //We don't need to wait for this, just want to start the interruption here

                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>Message sent, thanks.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                }
                else
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Content = new StringContent($"<html><body>You need to talk to the bot first so it can capture your details.</body></html>", System.Text.Encoding.UTF8, @"text/html");
                    return resp;
                }
        }

        private static async Task SendMessage(string textmessage, string originatingMessageId, string originatingRequestReeason, ConversationData data)
        {
            var userAccount = new ChannelAccount(data.toId, data.toName);
            var botAccount = new ChannelAccount(data.fromId, data.fromName);
            var connector = new ConnectorClient(new Uri(data.serviceUrl));

            // Create a new message.
            IMessageActivity message = Activity.CreateMessageActivity();

            // Set the address-related properties in the message and send the message.
            message.From = botAccount;
            message.Recipient = userAccount;
            message.Locale = "en-us";

            if (!string.IsNullOrEmpty(data.conversationId) && !string.IsNullOrEmpty(data.channelId))
            {
                // If conversation ID and channel ID was stored previously, use it.
                message.ChannelId = data.channelId;
                message.Conversation = new ConversationAccount(id: data.conversationId);
                var newCard = CardHelper.MakeRequestCard(originatingRequestReeason, textmessage);
                message.Attachments.Add(newCard.ToAttachment());
                await connector.Conversations.UpdateActivityAsync(data.conversationId, originatingMessageId, (Activity)message);
            }
            else
            {
                // Conversation ID was not stored previously, so create a conversation. 
                // Note: If the user has an existing conversation in a channel, this will likely create a new conversation window.
                data.conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
                message.Conversation = new ConversationAccount(id: data.conversationId);
                await connector.Conversations.SendToConversationAsync((Activity)message);
            }
        }
    }

    public class MessagePostDAO
    {
        public string conversationID { get; set; }
        public string msgType { get; set; }
        public string responseMsg { get; set; }
        public string originatingMessageId { get; set; }
        public string originatingRequestReason { get; set; }
    }
}