using MySql.Data.MySqlClient;
using System.Data.Common;

namespace FCMService
{
    public class MySQL
    {
        private readonly IConfiguration _configuration;

        public string connectToken { get; set; }
        public Dictionary<string, List<string>> serialTokenDic { get; set; }
        public List<string> serialList { get; set; }

        public MySQL(IConfiguration configuration)
        {
            serialTokenDic = new Dictionary<string, List<string>>();
            serialList = new List<string>();
            connectToken = Environment.GetEnvironmentVariable("DB_CONNECION") ?? Constants.DefaultConnectionString;
            _configuration = configuration;
        }
        public string ConnectionString
        {
            get { return _configuration.GetConnectionString("DefaultConnection"); }
        }

        public async Task WriteFcmToken(string serialNumber, string token)
        {
            string query = "INSERT INTO fcm (serial_number, fcm_token) VALUE (@SerialNumber, @Token);";
            using (MySqlConnection con = new MySqlConnection(connectToken))
            {
                await con.OpenAsync();
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@SerialNumber", serialNumber);
                    cmd.Parameters.AddWithValue("@Token", token);
                    if (cmd.ExecuteNonQuery() == 1)
                    {
                        Console.WriteLine("인서트 성공");
                    }
                    else
                    {
                        Console.WriteLine("인서트 실패");
                    }
                }
            }
        }

        public async Task ReadSensorLimitAsync()
        {
            string sql = "SELECT serial_number FROM fcm;";
            using (MySqlConnection con = new MySqlConnection(connectToken))
            {
                MySqlCommand cmd = new MySqlCommand(sql, con);
                await con.OpenAsync();
                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string serialNumber = reader["serial_number"].ToString();

                        if (!serialList.Contains(serialNumber))
                        {
                            serialList.Add(serialNumber);
                            Console.WriteLine(serialNumber);
                        }
                    }
                }
            }
        }

        public async Task ReadSerialNumberAsync()
        {
            string sql = "SELECT serial_number FROM fcm;";
            using (MySqlConnection con = new MySqlConnection(connectToken))
            {
                MySqlCommand cmd = new MySqlCommand(sql, con);
                await con.OpenAsync();
                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string serialNumber = reader["serial_number"].ToString();

                        if (!serialList.Contains(serialNumber))
                        {
                            serialList.Add(serialNumber);
                            Console.WriteLine(serialNumber);
                        }
                    }
                }
            }
        }

        public async Task ReadFcmTokenAsync()
        {
            serialTokenDic.Clear(); // 기존 데이터를 모두 지우고 다시 저장하기 위해 딕셔너리 초기화

            using (MySqlConnection con = new MySqlConnection(connectToken))
            {

                await con.OpenAsync();

                foreach (string serialNumber in serialList)
                {
                    List<string> tokenList = new List<string>();

                    string query = "SELECT fcm_token FROM fcm WHERE serial_number = @SerialNumber;";
                    using (MySqlCommand cmd = new MySqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SerialNumber", serialNumber);
                        using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string fcmToken = reader["fcm_token"].ToString();
                                tokenList.Add(fcmToken);
                            }
                        }
                        serialTokenDic.Add(serialNumber, tokenList);
                    }


                }
            }
        }
    }
}
