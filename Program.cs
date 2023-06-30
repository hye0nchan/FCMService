using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Timers;
using FirebaseAdmin;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Messaging;
using MySql.Data.MySqlClient;
using System.Data.Common;

namespace FCMService
{

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            MySQL mysql = new MySQL(configuration);
            Firebase firebase = new Firebase(mysql, configuration);
            MQTT mqtt = new MQTT(mysql, configuration);
            Scheduler scheduler = new Scheduler(mysql, mqtt);

            Console.OutputEncoding = Encoding.UTF8;
            await firebase.InitializationAsync();
            //await firebase.SendNotification("2305193Fg7");
            await mqtt.ConnectToCloudBroker();
            scheduler.Initialization();
        }
    }
}
