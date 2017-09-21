using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Linq;
using System;
using Microsoft.Bot.Connector.Teams.Models;
using System.Configuration;
using TimeOffBot.DAL;

namespace TimeOffBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        private static IUserManagerService _userService;

        public MessagesController()
        {
            var debugflag = ConfigurationManager.AppSettings["debugFlag"];
            _userService = new UserManagerService(Boolean.Parse(debugflag));
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new Dialogs.RouterDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                IConversationUpdateActivity update = message;
                var client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials());
                if (update.MembersAdded != null && update.MembersAdded.Any())
                {
                    foreach (var newMember in update.MembersAdded)
                    {
                        if (newMember.Id != message.Recipient.Id)
                        {
                            var reply = message.CreateReply();
                            reply.Text = $"Welcome {newMember.Name}! \nPlease use me to enter your time off, sickness and business travel requests. \nStart by saying hello.";
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }

                        TeamsChannelData currentChannelData = message.GetChannelData<TeamsChannelData>();
                        string tenantId = currentChannelData.Tenant.Id;
                        //string channelId = currentChannelData.Channel.Id;
                        string teamId = currentChannelData.Team.Id;
                        // Store in Doc DB
                        var bot = new BotRegistrationData()
                        {
                            tenantId = tenantId,
                            teamId = teamId
                        };

                        await _userService.RegisterBot(bot);

                        // set new registration

                        var channels = await client.GetTeamsConnectorClient().Teams
                                        .FetchChannelListWithHttpMessagesAsync(teamId);


                       
                        string channelId = channels.Body.Conversations[0].Id;
                         

                        var approvalChannelData = new ApprovalChannel()
                        {
                            channelId = channelId,
                            teamId = teamId,
                            tenantId = tenantId
                        };
                        approvalChannelData.channelId = channelId;
                        approvalChannelData.teamId = teamId;
                        var debugflag = ConfigurationManager.AppSettings["debugFlag"];
               
                        await _userService.SetApprovalChannel(approvalChannelData);
                           
                        
                        //StateClient stateClient = message.GetStateClient();
                        //BotData userData = await stateClient.BotState.GetUserDataAsync(message.ChannelId, message.From.Id);
                        //userData.SetProperty<ConversationData>("Conversation", conversation);
                        //await stateClient.BotState.SetUserDataAsync(message.ChannelId, message.From.Id, userData);
                    }
                } else
                {
 }

            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}