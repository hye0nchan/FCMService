namespace FCMService
{
    public static class Constants
    {
        public const string DefaultConnectionString = "Server=saltware.mooo.com;Port=3306;Database=USERDB;Uid=chan;Pwd=chan";
        public const string DefaultPath = @"newwayfarm-74ecc-firebase-adminsdk-2218b-71563d557f.json";
        public const string TopicFormat = "saltware/newwayFarm/{0}/#";
        public const string FcmTokenTopicFormat = "saltware/newwayFarm/FcmToken{0}/#";
        public const string RequestSensorFormat = "saltware/newwayFarm/{0}/request/sensor";
    }
}
