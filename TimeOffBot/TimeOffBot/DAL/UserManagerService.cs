using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TimeOffBot.DAL
{
    public interface IUserManagerService
    {
        ConversationData GetLatestConversation(string id);
        ConversationData GetConversation(string id, string messageId);
        ApprovalChannel GetApprovalChannel(string teamid);
        Task SetApprovalChannel(ApprovalChannel channel);
        BotRegistrationData GetBotRegistration(string tenantid);
        Task RegisterBot(BotRegistrationData botdata);

    }

    public class UserManagerService : IUserManagerService
    {
        //Read config
        private static readonly string endpointUrl = ConfigurationManager.AppSettings["docdb:EndpointUrl"];
        private static readonly string authorizationKey = ConfigurationManager.AppSettings["docdb:AuthorizationKey"];
        private static readonly string databaseId = ConfigurationManager.AppSettings["docdb:DatabaseName"];
        private static readonly string collectionId = ConfigurationManager.AppSettings["docdb:ConversationsCollectionName"];
        private static readonly ConnectionPolicy connectionPolicy = new ConnectionPolicy { UserAgentSuffix = " timeoffbot/2" };

        //Reusable instance of DocumentClient which represents the connection to a DocumentDB endpoint
        private static DocumentClient client;

        //The instance of a Database which we will be using for all the Collection operations being demo'd
        
        private static DocumentCollection docCollection;

        private static Dictionary<String, ConversationData> conversationDB = new Dictionary<String, ConversationData>();
        private static Dictionary<String, ApprovalChannel> approvalDB = new Dictionary<String, ApprovalChannel>();
        private static Dictionary<String, BotRegistrationData> botDB = new Dictionary<String, BotRegistrationData>();
        private static bool _debug;
        public UserManagerService(bool debug)
        {
            _debug = debug;
            if (debug)
            {

            } else
            {
                try
                {
                    if ((client == null) || (docCollection == null))
                    {
                        //Instantiate a new DocumentClient instance
                        client = new DocumentClient(new Uri(endpointUrl), authorizationKey, connectionPolicy);
                        docCollection = Initialize().Result;

                    }

                   
                }
                catch (DocumentClientException de)
                {
                    Exception baseException = de.GetBaseException();
                    Debug.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
                }
                catch (Exception e)
                {
                    Exception baseException = e.GetBaseException();
                    Debug.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
                }

            }

        }
        public async Task SaveConversation(ConversationData newConversation)
        {
            newConversation.type = "ConversationData";
            newConversation.creationDate = DateTime.UtcNow;
            if (_debug)
            {
                conversationDB.Add(newConversation.conversationId, newConversation);
            }
            else
            {
               try
                {
                await client.CreateDocumentAsync(docCollection.DocumentsLink, newConversation);

                } catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                
            }
        }
        public ConversationData GetConversation(string conversationId, string messageId)
        {
            ConversationData conversation = null;
            if (_debug)
            {
                conversation = conversationDB[conversationId];
            }
            else
            {
                IQueryable<ConversationData> query = client.CreateDocumentQuery<ConversationData>(docCollection.DocumentsLink)
                    .Where(con => (con.conversationId == conversationId) && (con.originatingMessage == messageId))
                    .OrderByDescending(d => d.creationDate);
                conversation = query.AsEnumerable().FirstOrDefault(); ;
            }
            return conversation;
        }
        public ConversationData GetLatestConversation(string conversationId)
        {
            ConversationData conversation = null;
            if (_debug)
            {
                conversation = conversationDB[conversationId];
            }
            else
            {
                IQueryable<ConversationData> query = client.CreateDocumentQuery<ConversationData>(docCollection.DocumentsLink)
                    .Where(con => con.conversationId == conversationId).OrderByDescending(d => d.creationDate);
                conversation = query.AsEnumerable().FirstOrDefault(); ;
            }
            return conversation;
        }

        public async Task SetApprovalChannel(ApprovalChannel approval)
        {
            approval.type = "ApprovalChannel";
            if (_debug)
            {
                approvalDB.Add(approval.teamId, approval);
            }
            else
            {
                try
                {
                    await client.CreateDocumentAsync(docCollection.DocumentsLink, approval);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
        public ApprovalChannel GetApprovalChannel(string tenantId)
        {
            ApprovalChannel conversation = null;
            if (_debug)
            {
                conversation = approvalDB[tenantId];
            }
            else
            {
                IQueryable<ApprovalChannel> query = client.CreateDocumentQuery<ApprovalChannel>(docCollection.DocumentsLink)
                    .Where(con => con.tenantId == tenantId);
                conversation = query.AsEnumerable().LastOrDefault();
            }
            return conversation;
        }

        public async Task RegisterBot(BotRegistrationData botdata)
        {
            botdata.type = "BotRegistrationData";
            if (_debug)
            {
                botDB.Add(botdata.tenantId, botdata);
            }
            else
            {
                try
                {
                    await client.CreateDocumentAsync(docCollection.DocumentsLink, botdata);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
        public BotRegistrationData GetBotRegistration(string tenantID)
        {
            BotRegistrationData botdata = null;
            if (_debug)
            {
                botdata = botDB[tenantID];
            }
            else
            {
                IQueryable<BotRegistrationData> query = client.CreateDocumentQuery<BotRegistrationData>(docCollection.DocumentsLink)
                    .Where(con => con.tenantId == tenantID);
                botdata = query.AsEnumerable().FirstOrDefault(); ;
            }
            return botdata;
        }


        private static async Task<DocumentCollection> Initialize()
        {
            Debug.WriteLine("Initializing Database");
            // Get the database by name, or create a new one if one with the name provided doesn't exist.
            // Create a query object for database, filter by name.
            IEnumerable<Database> query = from db in client.CreateDatabaseQuery()
                                          where db.Id == databaseId
                                          select db;

            // Run the query and get the database (there should be only one) or null if the query didn't return anything.
            // Note: this will run synchronously. If async exectution is preferred, use IDocumentServiceQuery<T>.ExecuteNextAsync.
            Database database = query.FirstOrDefault();
            if (database == null)
            {
                // Create the database.
                database = await client.CreateDatabaseAsync(new Database { Id = databaseId });
            }


            // Get the database by name, or create a new one if one with the name provided doesn't exist.
            // Create a query object for database, filter by name.
            IEnumerable<DocumentCollection> collectionQuery = from collection in client.CreateDocumentCollectionQuery(database.SelfLink)
                                            where collection.Id == collectionId
                                                              select collection;
            DocumentCollection docCollection = collectionQuery.FirstOrDefault();
            if (docCollection == null)
            {
                DocumentCollection c1 = await client.CreateDocumentCollectionAsync(database.SelfLink, new DocumentCollection { Id = collectionId });
                docCollection = c1;
            }
            return docCollection;
        }
        
    }

    public class ConversationData
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        public string type { get; set; }
        public string fromId { get; set; }
        public string fromName { get; set; }
        public string toId { get; set; }
        public string toName { get; set; }
        public string serviceUrl { get; set; }
        public string channelId { get; set; }
        public string conversationId { get; set; }
        public string tenantId { get; set; }
        public string originatingMessage { get; set; }
        public string teamId { get; set; }
        public string teamChannelId { get; set; }
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime creationDate { get; set; }
    }

    public class ApprovalChannel
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        public string type { get; set; }
        public string channelId { get; set; }
        public string teamId { get; set; }
        public string tenantId { get; set; }
    }

    public class BotRegistrationData
    {
        [JsonProperty(PropertyName = "id")]
        public string ID { get; set; }
        public string type { get; set; }
        public string tenantId { get; set; }
        public string teamId { get; set; }
    }

}