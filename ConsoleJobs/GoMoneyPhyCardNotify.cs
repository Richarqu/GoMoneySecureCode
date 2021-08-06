using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleJobs
{
    public class GoMoneyPhyCardNotify
    {
        public void GMPhysicalCardsNotification()
        {
            //InsertLastID();
            string ID = GetLastID();
            string productPhy = ConfigurationManager.AppSettings["ProductPhy"];
            string[] _productPhy = productPhy.Split('|');
            string _pronames = string.Empty;
            foreach (string item in _productPhy)
            {
                _pronames += item + ",";
            }
            _pronames = _pronames.Remove(_pronames.Length - 1, 1);

            string query = $"SELECT ID, Pan as card_number, Phone as phone_number, Account as account_number, 'GoMoney Mastercard' as card_provider FROM [EchanelsT24].[dbo].[CardRequest] where Product in ({_pronames}) and StatusFlag = '5' and ID > " + ID + " order by id asc";
            var cardNotifyResp = new CardNotifyResp();
            SqlDataReader sdr;
            try
            {
                var _configuration = ConfigurationManager.AppSettings["CardApp"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        sdr = cmd.ExecuteReader();
                        string i = string.Empty;
                        while (sdr.Read())
                        {
                            string pan = IBS_Decrypt(sdr["card_number"].ToString());
                            var payload = new CardNotifyReq
                            {
                                cards = new Card[]{
                                new Card{  account_number = sdr["account_number"].ToString(), card_number = pan.Substring(0,6)+"******"+pan.Substring(pan.Length - 4,4), card_provider = sdr["card_provider"].ToString(), phone_number = sdr["phone_number"].ToString()} }
                            };
                            CardNotify(payload);
                            i = string.Empty;
                            i = sdr["ID"].ToString();
                        }
                        if (!string.IsNullOrEmpty(i))
                        {
                            UpdateLastID(i);
                        }
                        cmd.Dispose();
                    }
                    connect.Dispose();
                    connect.Close();
                }
            }
            catch (Exception ex)
            {
                new ErrorLog("Exception at method GetVogueConfig: " + ex);
            }
        }
        public void CardNotify(CardNotifyReq cardData)
        {
            CardNotifyResp cardNotifyResp = new CardNotifyResp();
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["GoMoney_Base_Url"];
                string endPoint = ConfigurationManager.AppSettings["CardNotifyEndpoint"];
                string fullUri = $"{rootUrl}{endPoint}";
                var json = JsonConvert.SerializeObject(cardData);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpClient httpClient = new HttpClient())
                {
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                           | SecurityProtocolType.Tls11
                           | SecurityProtocolType.Tls12
                           | SecurityProtocolType.Ssl3;
                    httpClient.BaseAddress = new Uri(rootUrl);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = httpClient.PostAsync(fullUri, data).Result.Content.ReadAsStringAsync().Result;

                    var rawResp = JsonConvert.SerializeObject(response);
                    new ErrorLog("Raw response from reqeust is: " + rawResp);
                    cardNotifyResp = JsonConvert.DeserializeObject<CardNotifyResp>(response);
                    new ErrorLog("Json converted response for request" + json + "is: " + JsonConvert.SerializeObject(cardNotifyResp));
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex.Message);
            }
        }
        public string GetLastID()
        {
            string lastID = string.Empty;
            string query = $"SELECT [Value] FROM [EchanelsT24].[dbo].[Config] where Name = 'GomoneyPhyLastProcessed'";
            SqlDataReader sdr;
            int count;
            try
            {
                var _configuration = ConfigurationManager.AppSettings["CardApp"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        sdr = cmd.ExecuteReader();
                        count = sdr.FieldCount;
                        while (sdr.Read())
                        {
                            lastID = sdr["Value"].ToString();
                        }
                        cmd.Dispose();
                    }
                    connect.Dispose();
                    connect.Close();
                }
            }
            catch (Exception ex)
            {
                new ErrorLog($"Exception at method GetLastID: {ex}");
            }
            return lastID;
        }
        private void UpdateLastID(string iD)
        {
            int row = 0;
            try
            {
                var query = $"UPDATE [dbo].[Config] set Value = @iD where Name = 'GomoneyPhyLastProcessed'";
                var _configuration = ConfigurationManager.AppSettings["CardApp"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.Parameters.AddWithValue("@iD", iD);
                        row = cmd.ExecuteNonQuery();
                        connect.Dispose();
                        connect.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                new ErrorLog($"Exception at method UpdateLastID: {ex}");
            }
        }
        public void InsertLastID()
        {
            int i;
            try
            {
                var query = $"INSERT INTO [dbo].[Config] VALUES ('GomoneyPhyLastProcessed','');";
                var _configuration = ConfigurationManager.AppSettings["CardApp"];
                using (SqlConnection connect = new SqlConnection(_configuration))
                {
                    using (SqlCommand cmd = new SqlCommand(query, connect))
                    {
                        if (connect.State != ConnectionState.Open)
                        {
                            connect.Open();
                        }
                        cmd.CommandType = CommandType.Text;
                        i = cmd.ExecuteNonQuery();
                        connect.Dispose();
                        connect.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                new ErrorLog($"Exception at method InsertLastID: {ex}");
            }
        }
        public static string IBS_Decrypt(string val)
        {
            var pp = string.Empty;

            try
            {
                var sharedkeyval = "000000010000001000000011000001010000011100001011000011010001000100010010000100010000110100001011000001110000001100000100000010000000000100000010000000110000010100000111000010110000110100001101";
                sharedkeyval = BinaryToString(sharedkeyval);
                var sharedvectorval = "0000000100000010000000110000010100000111000010110000110100000011";
                sharedvectorval = BinaryToString(sharedvectorval);
                byte[] sharedkey = Encoding.GetEncoding("utf-8").GetBytes(sharedkeyval);
                byte[] sharedvector = Encoding.GetEncoding("utf-8").GetBytes(sharedvectorval);
                TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
                byte[] toDecrypt = Convert.FromBase64String(val);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, tdes.CreateDecryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
                cs.Write(toDecrypt, 0, toDecrypt.Length);
                cs.FlushFinalBlock();
                pp = Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                new ErrorLog("Error in method IBS_Decrypt" + ex);
                pp = val;
            }
            return pp;
        }

        private static string BinaryToString(string binary)
        {
            if (string.IsNullOrEmpty(binary))
                throw new ArgumentNullException("binary");

            if ((binary.Length % 8) != 0)
                throw new ArgumentException("Binary string invalid (must divide by 8)", "binary");

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < binary.Length; i += 8)
            {
                string section = binary.Substring(i, 8);
                int ascii = 0;
                try
                {
                    ascii = Convert.ToInt32(section, 2);
                }
                catch
                {
                    throw new ArgumentException("Binary string contains invalid section: " + section, "binary");
                }
                builder.Append((char)ascii);
            }
            return builder.ToString();
        }
        public class CardNotifyResp
        {
            public string status { get; set; }
            public Data data { get; set; }
        }

        public class Data
        {
            public string message { get; set; }
        }

        public class CardNotifyReq
        {
            public Card[] cards { get; set; }
        }

        public class Card
        {
            public string card_number { get; set; }
            public string phone_number { get; set; }
            public string account_number { get; set; }
            public string card_provider { get; set; }
        }
    }
}
