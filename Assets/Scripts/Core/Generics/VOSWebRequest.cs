using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace VOS.Core.Generics
{
    public class VOSWebRequest
    {
        public enum DataType
        {
            None = 0,
            JSON = 1
        }

        private static string GetDataTypeString(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.None:
                    return "";
                case DataType.JSON:
                    return "application/json";
                default:
                    return "";
            }
        }

        public static IEnumerator Get(string url, Action<string> onComplete, Action<string> onError = null)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(www.error);
                }
                else
                {
                    if (onComplete != null) onComplete(www.downloadHandler.text);
                }
            }
        }

        public static IEnumerator Post<T>(string url, T data, DataType type, Action<string> onComplete, Action<long, string> onError)
        {
            string dataString = "";
            switch (type)
            {
                case DataType.JSON:
                    dataString = JsonConvert.SerializeObject(data);
                    break;
                default:
                    break;
            }
            using (UnityWebRequest www = UnityWebRequest.Post(url, dataString, GetDataTypeString(type)))
            {
                yield return www.SendWebRequest();
           
                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(www.responseCode, www.error);
                }
                else
                {
                    if (onComplete != null) onComplete(www.downloadHandler.text);
                }
            }
        }

        public static IEnumerator Post_GetData<T>(string url, T data, DataType type, Action<byte[]> onComplete, Action<long, string> onError)
        {
            string dataString = "";
            switch (type)
            {
                case DataType.JSON:
                    dataString = JsonConvert.SerializeObject(data);
                    break;
                default:
                    break;
            }
            using (UnityWebRequest www = UnityWebRequest.Post(url, dataString, GetDataTypeString(type)))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (onError != null) onError(www.responseCode, www.error);
                }
                else
                {
                    if (onComplete != null) onComplete(www.downloadHandler.data);
                }
            }
        }
    }
}