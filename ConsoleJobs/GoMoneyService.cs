using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class GoMoneyService
    {
        public GoMoneyAcctInqRes Rest_Get(string acct_Num)
        {
            string str1 = string.Empty;
            GoMoneyAcctInqRes _resp = new GoMoneyAcctInqRes();
            try
            {
                string rootUrl = ConfigurationManager.AppSettings["GoMoney_Base_Url"];
                string endPoint = ConfigurationManager.AppSettings["Endpoint"];
                string fullUri = $"{rootUrl}{endPoint}{acct_Num}";

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
                    var response = httpClient.GetAsync(fullUri).Result.Content.ReadAsStringAsync().Result;

                    var rawResp = JsonConvert.SerializeObject(response);
                    new ErrorLog(rawResp);
                    _resp = JsonConvert.DeserializeObject<GoMoneyAcctInqRes>(response);
                    new ErrorLog(_resp.ToString());
                }
            }
            catch (Exception ex)
            {
                new ErrorLog(ex.Message);
            }
            return _resp;
        }
        public string Get_APIURL(string tran_Type)
        {
            string str = string.Empty;
            string empty = string.Empty;
            try
            {
                str = ConfigurationManager.AppSettings["GoMoney_Base_Url"] + ConfigurationManager.AppSettings[tran_Type];
            }
            catch (Exception ex)
            {
                new ErrorLog(ex.Message);
            }
            return str;
        }

        public string Get_Key()
        {
            string str = string.Empty;
            try
            {
                str = ConfigurationManager.AppSettings["GoMoney_SecretKey"];
            }
            catch (Exception ex)
            {
                new ErrorLog(ex.Message);
            }
            return str;
        }

        public string Get_Id()
        {
            string str = string.Empty;
            try
            {
                str = ConfigurationManager.AppSettings["GoMoney_Id"];
            }
            catch (Exception ex)
            {
                new ErrorLog(ex.Message);
            }
            return str;
        }

        public string HmacSHA512_enc(string message, string key)
        {
            using (HMACSHA512 hmacshA512 = new HMACSHA512(Encoding.UTF8.GetBytes(key)))
            {
                byte[] messageByte = GetMessageByte(message);
                return Convert.ToBase64String(hmacshA512.ComputeHash(messageByte));
            }
        }

        public string HmacSHA256_enc(string message, string key)
        {
            using (HMACSHA256 hmacshA256 = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                byte[] messageByte = GetMessageByte(message);
                return Convert.ToBase64String(hmacshA256.ComputeHash(messageByte));
            }
        }

        private static byte[] GetMessageByte(string msg)
        {
            return Encoding.UTF8.GetBytes(msg);
        }
    }
}
