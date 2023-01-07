using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PGL_Launcher
{
    public static class DownloadUtils
    {
        /// <summary>
        /// Downloads a string
        /// </summary>
        /// <param name="url">Download URL</param>
        /// <param name="method">HTTP method</param>
        /// <param name="payload">HTTP body</param>
        /// <param name="headers">Extra headers</param>
        /// <returns>Response string or null</returns>
        public static string DownloadString(string url, string method, byte[] payload = null, Dictionary<string, string> headers = null)
        {
            // Download
            try
            {
                HttpClient cl = new HttpClient();
                if (headers != null)
                    foreach (string key in headers.Keys)
                    {
                        cl.DefaultRequestHeaders.Add(key, headers[key]);
                    }
                if (method == "GET")
                    return cl.GetStringAsync(url).GetAwaiter().GetResult();
                else if (method == "POST")
                    return cl.PostAsync(url, new ByteArrayContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
                else
                    throw new ArgumentException("method");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a byte array
        /// </summary>
        /// <param name="url">Download URL</param>
        /// <param name="method">HTTP method</param>
        /// <param name="payload">HTTP body</param>
        /// <param name="headers">Extra headers</param>
        /// <returns>Response bytes or null</returns>
        public static byte[] DownloadBytes(string url, string method, byte[] payload = null, Dictionary<string, string> headers = null)
        {
            // Download
            try
            {
                HttpClient cl = new HttpClient();
                if (headers != null)
                    foreach (string key in headers.Keys)
                    {
                        cl.DefaultRequestHeaders.Add(key, headers[key]);
                    }
                if (method == "GET")
                    return cl.GetByteArrayAsync(url).GetAwaiter().GetResult();
                else if (method == "POST")
                    return cl.PostAsync(url, new ByteArrayContent(payload)).GetAwaiter().GetResult().Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                else
                    throw new ArgumentException("method");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Opens a download stream
        /// </summary>
        /// <param name="url">Download URL</param>
        /// <param name="method">HTTP method</param>
        /// <param name="payload">HTTP body</param>
        /// <param name="headers">Extra headers</param>
        /// <returns>Response stream or null</returns>
        public static Stream Download(string url, string method, out long max, byte[] payload = null, Dictionary<string, string> headers = null)
        {
            // Download
            try
            {
                WebRequest req = WebRequest.Create(url);
                req.Method = method;
                if (headers != null)
                    foreach (string key in headers.Keys)
                    {
                        req.Headers.Add(key, headers[key]);
                    }
                if (method == "GET")
                {
                    var res = req.GetResponse();
                    max = res.ContentLength;
                    return res.GetResponseStream();
                }
                else if (method == "POST")
                {
                    req.GetRequestStream().Write(payload, 0, payload.Length);
                    var res = req.GetResponse();
                    max = res.ContentLength;
                    return res.GetResponseStream();
                }
                else
                    throw new ArgumentException("method");
            }
            catch
            {
                max = -1;
                return null;
            }
        }
    }
}
