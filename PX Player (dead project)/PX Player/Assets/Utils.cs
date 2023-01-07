using Phoenix.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class Utils
{
    public static Stream DownloadAssets(string url)
    {
        if (Game.DigitalSeal == null)
        {
            return Download(url, "GET", null, new Dictionary<string, string>()
            {
                ["Session"] = Game.SessionToken == null ? "OFFLINE" : Game.SessionToken
            });
        }
        else
        {
            return Download(url, "GET", null, new Dictionary<string, string>()
            {
                ["Product-Key"] = Game.ProductKey,
                ["Digital-Seal"] = Game.DigitalSeal,
                ["Session"] = Game.SessionToken == null ? "OFFLINE" : Game.SessionToken
            });
        }
    }
    public static Stream Download(string url, string method = "GET", string payload = null, Dictionary<string, string> headers = null)
    {
        // Log
        Console.WriteLine("Downloading: " + url + "...");

        Console.WriteLine("Request details:");
        Console.WriteLine("URL: " + url);
        Console.WriteLine("Method: " + method);
        if (headers != null)
        {
            foreach ((string key, string value) in headers)
            {
                Console.WriteLine("[" + key + "]: " + value);
            }
        }

        // Download
        try
        {
            HttpClient cl = new HttpClient();
            if (headers != null)
                foreach ((string key, string value) in headers)
                {
                    cl.DefaultRequestHeaders.Add(key, value);
                }
            if (method == "GET") {
                var res = cl.GetAsync(url).GetAwaiter().GetResult();
                if (!res.IsSuccessStatusCode)
                    return null;
                return res.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            else if (method == "POST")
            {
                var res = cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult();
                if (!res.IsSuccessStatusCode)
                    return null;
                return res.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            }
            else
                throw new ArgumentException("method");
        }
        catch
        {
            return null;
        }
    }
    public static string DownloadString(string url, string method = "GET", string payload = null, Dictionary<string, string> headers = null)
    {
        // Log
        Console.WriteLine("Downloading: " + url + "...");

        Console.WriteLine("Request details:");
        Console.WriteLine("URL: " + url);
        Console.WriteLine("Method: " + method);
        if (headers != null)
        {
            foreach ((string key, string value) in headers)
            {
                Console.WriteLine("[" + key + "]: " + value);
            }
        }

        // Download
        try
        {
            HttpClient cl = new HttpClient();
            if (headers != null)
                foreach ((string key, string value) in headers)
                {
                    cl.DefaultRequestHeaders.Add(key, value);
                }
            if (method == "GET")
                return cl.GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            else if (method == "POST")
                return cl.PostAsync(url, new StringContent(payload)).GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            else
                throw new ArgumentException("method");
        }
        catch
        {
            return null;
        }
    }

}
