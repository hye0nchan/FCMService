using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace FCMService
{
    public class MQTT
    {
        private readonly IConfiguration _configuration;
        public uPLibrary.Networking.M2Mqtt.MqttClient mqttClient;
        private string clientId;
        private string serialNumber;
        private string brokerAddress;
        private MySQL mysql;

        public MQTT(MySQL mysql, IConfiguration configuration)
        {
            this.mysql = mysql;
            brokerAddress = "saltware.mooo.com";
            mqttClient = new uPLibrary.Networking.M2Mqtt.MqttClient(brokerAddress, 1884, false, null, null, uPLibrary.Networking.M2Mqtt.MqttSslProtocols.None);
            clientId = Guid.NewGuid().ToString();
            this._configuration = configuration;
        }


        public async Task ConnectToCloudBroker()
        {
            try
            {
                mqttClient.Connect(clientId);
                mqttClient.MqttMsgPublishReceived += MqttReceiveHandler;
                mqttClient.ConnectionClosed += ClientConnectionClosed;

                mqttClient.Subscribe(new string[] { "saltware/newwayFarm/FcmToken" + serialNumber + "/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                foreach (string serialNumber in mysql.serialList)
                {
                    mqttClient.Subscribe(new string[] { "saltware/newwayFarm/" + serialNumber + "/#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                }

                Console.WriteLine("Connected to MQTT broker.");
                await Task.CompletedTask;

            }


            catch (Exception e)
            {

                System.Console.WriteLine(e);
                System.Console.WriteLine("브로커 연결실패");
                throw;

            }
        }

        public async Task ReConnectToCloudMqttBroker()
        {
            while (true)
            {
                try
                {
                    await ConnectToCloudBroker();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to connect to MQTT broker: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }

        private void MqttReceiveHandler(object sender, MqttMsgPublishEventArgs e)
        {
            string receivedMessage = Encoding.UTF8.GetString(e.Message);
            string receivedTopic = e.Topic.ToString();
            Console.WriteLine("message: " + receivedMessage);
            Console.WriteLine("topic: " + receivedTopic);

            if (mqttClient == null) return;

            try
            {
                if (receivedTopic.Contains("FcmToken"))
                {
                    serialNumber = receivedTopic.Substring(receivedTopic.LastIndexOf('/') + 1);

                    mysql.WriteFcmToken(serialNumber, receivedMessage);
                }

                else
                {
                    //Console.WriteLine(receivedMessage);
                    List<SensorValue> sensorValues= JsonConvert.DeserializeObject<List<SensorValue>>(receivedMessage);

                    foreach (SensorValue sv in sensorValues)
                    {
                        System.Console.WriteLine("Sensor ID: " + sv.sensor_id + ", Value: " + sv.value);
                    }


                    //Console.WriteLine(jsonData.value);
                }

            }
            catch (Exception ee)
            {
                Console.WriteLine($"client_MqttMsgPublishReceived 실패 e:{ee}");
            }
        }

        private async void ClientConnectionClosed(object sender, EventArgs e)
        {
            Console.WriteLine("MQTT 재연결");
            ReConnectToCloudMqttBroker();
        }

    }
}
