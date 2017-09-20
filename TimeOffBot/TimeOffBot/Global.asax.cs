using Autofac;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using TimeOffBot.Dialogs;

namespace TimeOffBot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {

            string endpointUrl = ConfigurationManager.AppSettings["docdb:EndpointUrl"];
            string authorizationKey = ConfigurationManager.AppSettings["docdb:AuthorizationKey"];
            string databaseId = ConfigurationManager.AppSettings["docdb:DatabaseName"];
            string collectionId = ConfigurationManager.AppSettings["docdb:ConversationsCollectionName"];

            Uri docDbEndPointUri = new Uri(endpointUrl);

            //Fixed docDb emulator key

            // http://docs.autofac.org/en/latest/integration/webapi.html#quick-start
            var builder = new ContainerBuilder();

            // register the Bot Builder module
            builder.RegisterModule(new DialogModule());
            // register the alarm dependencies
            builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

            builder
                .RegisterInstance(new RootDialog())
                .As<IDialog<object>>();

            var store = new DocumentDbBotDataStore(docDbEndPointUri, authorizationKey);
            builder.Register(c => store)
                .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                .AsSelf()
                .SingleInstance();

            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
