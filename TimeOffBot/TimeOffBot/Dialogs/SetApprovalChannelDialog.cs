using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Connector;
using TimeOffBot.DAL;
using System.Configuration;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class SetApprovalChannelDialog : IDialog
    {
        public async Task StartAsync(IDialogContext context)
        {
            var connector = new ConnectorClient(new Uri(context.Activity.ServiceUrl));

            // get channel
    

            var channels = await connector.GetTeamsConnectorClient().Teams.FetchChannelListWithHttpMessagesAsync(context.Activity.GetChannelData<TeamsChannelData>().Team.Id);


            TeamsChannelData currentChannelData = context.Activity.GetChannelData<TeamsChannelData>();
            string tenantId = currentChannelData.Tenant.Id;
            string channelId = currentChannelData.Channel.Id;
            string teamId = currentChannelData.Team.Id;

            var approvalChannelData = new ApprovalChannel()
            {
                channelId = channelId,
                teamId = teamId,
                tenantId = tenantId
            };
            approvalChannelData.channelId = channelId;
            approvalChannelData.teamId = teamId;
            var debugflag = ConfigurationManager.AppSettings["debugFlag"];
            var _userService = new UserManagerService(Boolean.Parse(debugflag));
            await _userService.SetApprovalChannel(approvalChannelData);

            // post to channel and store in docdb
            var channelData = new TeamsChannelData { Channel = new ChannelInfo(channelId) };
            IMessageActivity newMessage = Activity.CreateMessageActivity();
            newMessage.Type = ActivityTypes.Message;
            newMessage.Text = "All new approvals will be routed to this channel.";
            ConversationParameters conversationParams = new ConversationParameters(
                isGroup: true,
                bot: null,
                members: null,
                topicName: "Approval channel set",
                activity: (Activity)newMessage,
                channelData: channelData);

            var msgresult = await connector.Conversations.CreateConversationAsync(conversationParams);
        }

      
    }
}