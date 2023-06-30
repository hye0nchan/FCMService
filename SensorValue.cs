namespace FCMService
{
    public class SensorValue
    {
        public SensorValue(int sensor_id, double value)
        {
            this.sensor_id = sensor_id;
            this.value = Math.Round(value, 2); // 반올림
        }

        public int sensor_id { get; set; }
        public double value { get; set; }
    }
}
