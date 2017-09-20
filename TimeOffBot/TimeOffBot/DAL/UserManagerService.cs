using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
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
        ConversationData GetConversation(string id);
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
        private static Database database;
        private static DocumentCollection docCollection;

        private static Dictionary<String, ConversationData> debugDB = new Dictionary<String, ConversationData>();
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
                    //Instantiate a new DocumentClient instance
                    using (client = new DocumentClient(new Uri(endpointUrl), authorizationKey, connectionPolicy))
                    {
                        //Get, or Create, a reference to Database
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
        public void SaveConversation(ConversationData newConversation)
        {
            if (_debug)
            {
                debugDB.Add(newConversation.conversationId, newConversation);
            }
            else
            {

            }
        }
        public ConversationData GetConversation(string conversationId)
        {
            ConversationData conversation = null;
            if (_debug)
            {
                conversation = debugDB[conversationId];
            }
            else
            {

            }
            return conversation;
        }


        
        private static async Task<DocumentCollection> Initialize()
        {
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

        public string fromId { get; set; }
        public string fromName { get; set; }
        public string toId { get; set; }
        public string toName { get; set; }
        public string serviceUrl { get; set; }
        public string channelId { get; set; }
        public string conversationId { get; set; }
    }


}