using System.Text;
using System.Timers;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace FCMService
{
    public class Scheduler
    {
        public static System.Timers.Timer? schedulerTimer { get; set; }
        public MySQL mysql { get; set; }
        public MQTT mqtt { get; set; }

        public Scheduler(MySQL mysql, MQTT mqtt)
        {
            this.mysql = mysql;
            this.mqtt = mqtt;
        }

        public void Initialization()
        {
            schedulerTimer = new System.Timers.Timer(3000);
            schedulerTimer.Elapsed += ScheduleEvent;
            schedulerTimer.AutoReset = true;
            schedulerTimer.Enabled = true;
        }

        public void ScheduleEvent(Object? source, ElapsedEventArgs e)
        {

            try
            {
                foreach (string serialNumber in mysql.serialList)
                {
                    mqtt.mqttClient.Publish("saltware/newwayFarm/" + serialNumber + "/request/sensor", Encoding.UTF8.GetBytes("jsonString"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    Thread.Sleep(3000);
                }

            }

            catch (Exception ex)
            {

            }
        }

    }
}
