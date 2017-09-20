using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using TimeOffBot.Dialogs;
using TimeOffBot.DAL;

namespace TimeOffBot.Utils
{
    public class ConversationStarter
    {
        //Note: Of course you don't want this here. Eventually you will need to save this in some table
        //Having this here as static variable means we can only remember one user :)
        //public static string conversationReference;


        public static async Task CheckForUser(string channelid)
        {

        }

        public static async Task SendMessage(string textmessage, ConversationData data)
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
            message.Text = textmessage;
            message.Locale = "en-us";
            await connector.Conversations.SendToConversationAsync((Activity)message);

        }
        
        
    }
}