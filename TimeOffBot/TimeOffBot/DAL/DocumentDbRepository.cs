using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace TimeOffBot.DAL
{

        public class DocumentDBRepository<T> where T : Document
        {
            private EventsDBOptions _options;
            public DocumentDBRepository(EventsDBOptions options)
            {
                _options = options;
            }
            public T Get(Expression<Func<T, bool>> predicate)
            {
                return Client.CreateDocumentQuery<T>(Collection.DocumentsLink)
                            .Where(predicate)
                            .AsEnumerable()
                            .FirstOrDefault();
            }

            public T GetById(string id)
            {
                T doc = Client.CreateDocumentQuery<T>(Collection.SelfLink)
                    .Where(d => d.Id == id)
                    .AsEnumerable()
                    .FirstOrDefault();

                return doc;
            }

            public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
            {
                var ret = Client.CreateDocumentQuery<T>(Collection.SelfLink)
                    .Where(predicate)
                    .AsEnumerable();

                return ret;
            }

            public async Task<T> CreateAsync(T entity)
            {
                Document doc = await Client.CreateDocumentAsync(Collection.SelfLink, entity);
                T ret = (T)(dynamic)doc;
                return ret;
            }

            public async Task<Document> UpdateAsync(string id, T entity)
            {
                Document doc = GetById(id);
                return await Client.ReplaceDocumentAsync(doc.SelfLink, entity);
            }

            public async Task DeleteAsync(string id)
            {
                Document doc = GetById(id);
                await Client.DeleteDocumentAsync(doc.SelfLink);
            }

            private string databaseId;
            public String DatabaseId
            {
                get
                {
                    if (string.IsNullOrEmpty(databaseId))
                    {

                        databaseId = _options.DatabaseName;
                    }

                    return databaseId;
                }
            }

            private string collectionId;
            public String CollectionId
            {
                get
                {
                    if (string.IsNullOrEmpty(collectionId))
                    {

                        collectionId = _options.ConversationsCollectionName;
                    }

                    return collectionId;
                }
            }

            private Database database;
            private Database Database
            {
                get
                {
                    if (database == null)
                    {

                        database = GetOrCreateDatabase(DatabaseId);
                    }

                    return database;
                }
            }

            private DocumentCollection collection;
            private DocumentCollection Collection
            {
                get
                {
                    if (collection == null)
                    {
                        collection = GetOrCreateCollection(Database.SelfLink, CollectionId);
                    }

                    return collection;
                }
            }

            private DocumentClient client;
            private DocumentClient Client
            {
                get
                {
                    if (client == null)
                    {
                        string endpoint = _options.EndpointUrl;
                        string authKey = _options.AuthorizationKey;

                        //the UserAgentSuffix on the ConnectionPolicy is being used to enable internal tracking metrics
                        //this is not requirted when connecting to DocumentDB but could be useful if you, like us, want to run 
                        //some monitoring tools to track usage by application
                        ConnectionPolicy connectionPolicy = new ConnectionPolicy { UserAgentSuffix = " samples-net-searchabletodo/1" };

                        client = new DocumentClient(new Uri(endpoint), authKey, connectionPolicy);
                    }

                    return client;
                }
            }

            public DocumentCollection GetOrCreateCollection(string databaseLink, string collectionId)
            {
                var col = Client.CreateDocumentCollectionQuery(databaseLink)
                                  .Where(c => c.Id == collectionId)
                                  .AsEnumerable()
                                  .FirstOrDefault();

                if (col == null)
                {
                    col = client.CreateDocumentCollectionAsync(databaseLink,
                        new DocumentCollection { Id = collectionId },
                        new RequestOptions { OfferType = "S1" }).Result;
                }

                return col;
            }
            public Database GetOrCreateDatabase(string databaseId)
            {
                var db = Client.CreateDatabaseQuery()
                                .Where(d => d.Id == databaseId)
                                .AsEnumerable()
                                .FirstOrDefault();

                if (db == null)
                {
                    db = client.CreateDatabaseAsync(new Database { Id = databaseId }).Result;
                }

                return db;
            }
        }

    public class EventsDBOptions
    {
        public string DatabaseName { get; set; }
        public string ConversationsCollectionName { get; set; }

        public string EndpointUrl { get; set; }
        public string AuthorizationKey { get; set; }
    }

}