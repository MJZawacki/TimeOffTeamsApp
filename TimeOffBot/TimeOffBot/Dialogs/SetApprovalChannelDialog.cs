using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Connector;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class SetApprovalChannelDialog : IDialog
    {
        public async Task StartAsync(IDialogContext context)
        {
            var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));

            var activity = context.Activity;
             var response = await connector.GetTeamsConnectorClient().Teams.FetchChannelListWithHttpMessagesAsync
                (activity.GetChannelData<TeamsChannelData>().Team.Id);
            var choices = new Dictionary<string, IReadOnlyList<string>>();
            var descriptions = new List<string>();
            choices.Add("General", new List<string> { "General" });
            descriptions.Add("General");
            foreach (var channel in response.Body.Conversations)
            {
                if (channel.Name != null)
                {
                    choices.Add(channel.Name, new List<string> { channel.Name });
                    descriptions.Add(channel.Name);
                } else
                {
                    choices.Add("General", new List<string> { channel.Id });
                    descriptions.Add("General");
                }
            }

            // Create Card with Channel List
            await Task.Run(() =>
            {
              

                var promptOptions = new PromptOptionsWithSynonyms<string>(
                    $"Hey, what channel do you want to select?",
                    "I am sorry but I didn't understand that. I need you to select one of the options below",
                    choices: choices,
                    descriptions: descriptions,
                    speak: $"Hey, what channel do you want to select?",
                    retrySpeak: "I am sorry but I didn't get that. Please say the name of a channel",
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
                // post to channel and store in docdb
                var channelData = new TeamsChannelData { Channel = new ChannelInfo(yourChannelId) };
                IMessageActivity newMessage = Activity.CreateMessageActivity();
                newMessage.Type = ActivityTypes.Message;
                newMessage.Text = "Hello, on a new thread";
                ConversationParameters conversationParams = new ConversationParameters(
                    isGroup: true,
                    bot: null,
                    members: null,
                    topicName: "Test Conversation",
                    activity: (Activity)newMessage,
                    channelData: channelData);
                var result = await connector.Conversations.CreateConversationAsync(conversationParams);

            }
            catch (TooManyAttemptsException)
            {
                await this.StartAsync(context);
            }
        }

    }
}