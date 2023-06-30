using FirebaseAdmin.Messaging;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System.Text;
using System.Timers;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace FCMService
{
    public class Firebase
    {
        private readonly IConfiguration _configuration;
        public MySQL mysql { get; set; }
        public FirestoreDb db { get; set; }

        public Firebase(MySQL mysql, IConfiguration configuration)
        {
            this.mysql = mysql;
            _configuration = configuration;
        }

        public async Task InitializationAsync()
        {
            string path = Environment.GetEnvironmentVariable("CREDENTIALS_PATH") ?? Constants.DefaultPath;
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);
            db = FirestoreDb.Create("newwayfarm-74ecc");
            mysql.ReadSerialNumberAsync();
            await mysql.ReadFcmTokenAsync();
        }


        public async Task SendNotification(string serialNumber)
        {

            List<string> tokenList = mysql.serialTokenDic.ContainsKey(serialNumber) ? mysql.serialTokenDic[serialNumber] : new List<string>();
            FirebaseApp app = null;

            try
            {
                app = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("newwayfarm-74ecc-firebase-adminsdk-2218b-71563d557f.json")
                });

                var messaging = FirebaseMessaging.GetMessaging(app);

                foreach (string token in tokenList)
                {
                    var message = new Message
                    {
                        Token = token,
                        Notification = new Notification
                        {
                            Title = "Title",
                            Body = "Message body"
                        }
                    };

                    string response = await messaging.SendAsync(message);
                    Console.WriteLine($"FCM message sent. Response: {response}");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending FCM message: {ex.Message}");
            }
            finally
            {
                if (app != null)
                    app.Delete();
            }

        }

    }
}
