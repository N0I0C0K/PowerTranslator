using System;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.Serialization.Json;

namespace Translater.utils
{
    public class ResItems
    {
        public string src;
        public string dst;
    }
    public class TranslateResponse
    {

        public string from;
        public string to;
        public int error_code = 52000;
        public List<ResItems> trans_result;
    }

    public class Utils
    {
        public static string appid = "20211228001040502";
        public static string secretKey = "Htvq0G7dc2M2MIjHzTYH";
        public static string GETHttp(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.UserAgent = null;
            request.Timeout = 6000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        public static TranslateResponse TranslateZhToEn(string query)
        {
            string from = "zh", to = "en";
            string salt = new Random().Next(100000).ToString();
            string sign = EncryptString(appid + query + salt + secretKey);
            string url = string.Format("http://api.fanyi.baidu.com/api/trans/vip/translate?q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                                        query, from, to, appid, salt, sign);
            string res = GETHttp(url);
            TranslateResponse data = null;
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(res)))
            {
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(TranslateResponse));
                data = (TranslateResponse)jsonSerializer.ReadObject(memoryStream);
            }
            return data;
        }
        public static string TranslateZhToEnRaw(string query)
        {
            string from = "zh", to = "en";
            string salt = new Random().Next(100000).ToString();
            string sign = EncryptString(appid + query + salt + secretKey);
            string url = string.Format("http://api.fanyi.baidu.com/api/trans/vip/translate?q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                                        query, from, to, appid, salt, sign);
            string res = GETHttp(url);
            return res;
        }
        public static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }
            // 返回加密的字符串
            return sb.ToString();
        }
    }
}