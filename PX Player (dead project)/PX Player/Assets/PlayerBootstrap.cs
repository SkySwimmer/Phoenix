using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System;
using TMPro;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using Phoenix.Common;
using Phoenix.Common.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Mono.Cecil;

public class PlayerBootstrap : MonoBehaviour
{
    private Image progressBar;
    private Image progressBarMain;
    private RectTransform progressBarT;
    private bool wait = false;
    private bool ready = false;
    private long max;
    private float progress;

    // Start is called before the first frame update
    void Start()
    {
        RawImage img = GetComponent<RawImage>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
        img = GameObject.Find("/Canvas/Panel/GameLogo").GetComponent<RawImage>();
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
        progressBarMain = GameObject.Find("/Canvas/Panel/ProgressBar").GetComponent<Image>();
        progressBarMain.color = new Color(progressBarMain.color.r, progressBarMain.color.g, progressBarMain.color.b, 0);
        progressBar = GameObject.Find("/Canvas/Panel/ProgressBar/Progress").GetComponent<Image>();
        progressBar.color = new Color(progressBar.color.r, progressBar.color.g, progressBar.color.b, 0);
        progressBarT = GameObject.Find("/Canvas/Panel/ProgressBar/Progress").GetComponent<RectTransform>();
        progressBarT.localScale = new Vector3(0, 1, 1);

        // Parse arguments
        Dictionary<string, string> arguments = new Dictionary<string, string>();
        string[] args = Environment.GetCommandLineArgs();
        if (Application.isEditor)
        {
            args = new string[] { "--play", "--gamedoc", "12345" };
        }
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--"))
            {
                string argS = arg.Substring(2);
                if (argS.Contains("="))
                {
                    string key = argS.Remove(argS.IndexOf("="));
                    string value = argS.Substring(argS.IndexOf("=") + 1);
                    arguments[key] = value;
                }
                else
                {
                    if (argS == "play")
                    {
                        arguments["play"] = "true";
                        continue;
                    }
                    if (i + 1 < args.Length)
                    {
                        arguments[argS] = args[i + 1];
                        i++;
                    }
                    else
                    {
                        ErrorWindow("Invalid argument: " + arg + ": missing value.");
                        FadeIn();
                        return;
                    }
                }
            }
        }

        // Check arguments
        if (!arguments.ContainsKey("play") || !arguments.ContainsKey("gamedoc"))
        {
            ErrorWindow("Unable to start with missing arguments, please use the launcher.");
            FadeIn();
            return;
        }

        FadeIn();

        // Start
        StartCoroutine(Startup(arguments));
    }

    void ErrorWindow(string message)
    {
        GameObject.Find("/Canvas").FindObject("ErrorWindow").SetActive(true);
        TextMeshProUGUI text = GameObject.Find("/Canvas").FindObject("ErrorWindow").FindObject("ErrorMessage").GetComponent<TextMeshProUGUI>();
        text.text = message;
    }

    void ComepleteFadeOut()
    {
        // Load Image component as Image object
        RawImage img = GetComponent<RawImage>();
        RawImage img2 = GameObject.Find("/Canvas/Panel/GameLogo").GetComponent<RawImage>();

        // Tween
        LeanTween.value(gameObject, 1, 0, 0.7f).setOnUpdate(f =>
        {
            progressBar.color = new Color(progressBar.color.r, progressBar.color.g, progressBar.color.b, f);
            progressBarMain.color = new Color(progressBarMain.color.r, progressBarMain.color.g, progressBarMain.color.b, f);
            img.color = new Color(img.color.r, img.color.g, img.color.b, f);
            img2.color = new Color(img2.color.r, img2.color.g, img2.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
        });
    }
    void FadeIn()
    {
        // Load Image component as Image object
        RawImage img = GetComponent<RawImage>();

        // Tween
        LeanTween.value(gameObject, 0, 1, 0.3f).setOnUpdate(f =>
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, f);
        }).setOnComplete(() =>
        {
            if (!ready)
                FadeOut();
        });
    }
    void FadeInGameLogo()
    {
        // Load Image component as Image object
        RawImage img = GameObject.Find("/Canvas/Panel/GameLogo").GetComponent<RawImage>();

        // Tween
        LeanTween.value(gameObject, 0, 1, 0.3f).setOnUpdate(f =>
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
        });
    }
    void FadeOutGameLogo()
    {
        // Load Image component as Image object
        RawImage img = GameObject.Find("/Canvas/Panel/GameLogo").GetComponent<RawImage>();

        // Tween
        LeanTween.value(gameObject, 1, 0, 0.3f).setOnUpdate(f =>
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
        });
    }
    void FadeInProgressBar()
    {
        ready = false;

        // Tween
        LeanTween.value(gameObject, 0, 1, 0.3f).setOnUpdate(f =>
        {
            progressBar.color = new Color(progressBar.color.r, progressBar.color.g, progressBar.color.b, f);
            progressBarMain.color = new Color(progressBarMain.color.r, progressBarMain.color.g, progressBarMain.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
        });
    }
    void FadeOutProgressBar()
    {
        ready = false;

        // Tween
        LeanTween.value(gameObject, 1, 0, 0.7f).setOnUpdate(f =>
        {
            progressBar.color = new Color(progressBar.color.r, progressBar.color.g, progressBar.color.b, f);
            progressBarMain.color = new Color(progressBarMain.color.r, progressBarMain.color.g, progressBarMain.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
            progressBarT.localScale = new Vector3(0, 1, 1);
        });
    }
    void FadeOutProgressBarValue()
    {
        ready = false;

        // Tween
        LeanTween.value(gameObject, 1, 0, 0.7f).setOnUpdate(f =>
        {
            progressBar.color = new Color(progressBar.color.r, progressBar.color.g, progressBar.color.b, f);
        }).setOnComplete(() =>
        {
            ready = true;
            progressBarT.localScale = new Vector3(0, 1, 1);
        });
    }
    void SetProgressBarValue(float progress, long max)
    {
        ready = false;
        double step = 1d / (double)max;

        // Tween
        LeanTween.value(gameObject, progressBarT.localScale.x, (float)(step * progress), 0.3f).setOnUpdate(f =>
        {
            progressBarT.localScale = new Vector3(f, 1, 1);
        }).setOnComplete(() =>
        {
            ready = true;
        });
    }


    void FadeOut()
    {
        // Load Image component as Image object
        RawImage img = GetComponent<RawImage>();

        // Tween
        LeanTween.value(gameObject, 1, 0, 0.7f).setOnUpdate(f =>
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, f);
        }).setOnComplete(() =>
        {
            if (wait)
            {
                ready = true;
                wait = false;
                return;
            }
            StartCoroutine(WaitBeforeFade(0.5f));
        });
    }

    IEnumerator Startup(Dictionary<string, string> arguments)
    {
        yield return new WaitForSeconds(2);

        Dictionary<string, string> game = new Dictionary<string, string>();
        yield return Task.Run(() =>
        {
            // Download
            TcpClient cl = new TcpClient("127.0.0.1", int.Parse(arguments["gamedoc"]));
            MemoryStream strm = new MemoryStream();
            while (true)
            {
                try
                {
                    int i = cl.GetStream().ReadByte();
                    if (i == -1)
                        break;
                    strm.WriteByte((byte)i);
                }
                catch
                {
                    break;
                }
            }
            byte[] data = strm.ToArray();

            // Decode
            string gameData = Encoding.UTF8.GetString(data);
            foreach (string line in gameData.Split("\n"))
            {
                if (line == "")
                    continue;
                string key = line.Remove(line.IndexOf(": "));
                string value = line.Substring(line.IndexOf(": ") + 2);
                game[key] = value;
            }

            // Initialize
            Game.Init(game);
            game["Assembly-Package-File"] = game["Assembly-Package-File"].Replace("$<platform>", Environment.OSVersion.Platform == PlatformID.Unix ? "linux64" : "win64");
            game["Prestart-Package-File"] = game["Prestart-Package-File"].Replace("$<platform>", Environment.OSVersion.Platform == PlatformID.Unix ? "linux64" : "win64");

            // Prepare directories
            Directory.CreateDirectory(Game.AssetsFolder);
            Directory.CreateDirectory(Game.GameFiles);
            Directory.CreateDirectory(Game.PlayerData);

            Debug.Log("Game information received!");
            Debug.Log("Game: " + Game.Title + " (id: " + Game.GameID + ")");
            Debug.Log("Version: " + Game.Version);
            Debug.Log("Game data folder: " + Game.GameFiles);
            Debug.Log("Asset data folder: " + Game.AssetsFolder);
            Debug.Log("Player data folder: " + Game.PlayerData);
            Debug.Log("Session token (showing payload only): " + (Game.SessionToken == null ? "OFFLINE" : Game.SessionToken.Split(".")[1]));
            ready = true;
        });
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        string sessionToken = game["Session"];
        ready = false;

        // Contact refresh endpoint
        string newToken = Utils.DownloadString(game["Refresh-Endpoint"], "GET", null, new Dictionary<string, string>()
        {
            ["Authorization"] = "Bearer " + sessionToken
        });

        // Verify access
        if (newToken != null && newToken != "")
            game["Session"] = newToken;
        else
        {
            Debug.LogWarning("Entering offline mode...");
            if (!Game.OfflineSupport)
            {
                // Error screen
                Debug.LogError("Entering offline mode failed: no offline play support.");
                ErrorWindow("Please connect your device to the internet before continuing.");
                yield break;
            }
        }

        // Token refresh
        Game.InitTokenRefresh(sessionToken, game["Refresh-Endpoint"]);

        // Download main assembly package
        string url = game["Assembly-Package-File"];
        if (!url.StartsWith("file:") && !url.StartsWith("http:") && !url.StartsWith("https:"))
        {
            if (!url.StartsWith("/"))
                url = "/" + url;
            url = game["Data-Download-Base-Url"] + url;
        }
        url = url.Replace("$<platform>", Environment.OSVersion.Platform == PlatformID.Unix ? "linux64" : "win64");
        Debug.Log("Downloading assembly package...");
        Stream asmsStr = Utils.DownloadAssets(url);
        if (asmsStr == null)
        {
            Debug.LogWarning("Download rejected.");

            // We are offline
            game["Session"] = "OFFLINE";

            // Find cached
            if (File.Exists(Game.GameFiles + "/" + game["Assembly-Package-File"]))
            {
                Debug.Log("Loading cached assembly package...");
                asmsStr = File.OpenRead(Game.GameFiles + "/" + game["Assembly-Package-File"]);
            }
            else
            {
                Debug.LogError("Could not download or load a cached assembly package.");

                // Error screen
                ErrorWindow("Failed to download primary assemply package.\nUnable to start the game.\n\nError: server rejected download request.\nA connection error is likely the cause.\n\nGame: " + Game.Title + "\nVersion: " + Game.Version);
                yield break;
            }

            Game.Init(game);
        }

        // Download
        wait = true;
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        FadeInProgressBar();
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        max = 100;
        StartCoroutine(ProgressBar());
        progress = 1;
        if (!(asmsStr is FileStream))
        {
            yield return Task.Run(() =>
            {
                Stream outP = File.OpenWrite(Game.GameFiles + "/" + game["Assembly-Package-File"]);
                long l = asmsStr.Length;
                max = l;
                for (long i = 0; i < l; i++)
                {
                    progress = i + 1;
                    outP.WriteByte((byte)asmsStr.ReadByte());
                }
                asmsStr.Close();
                outP.Close();
                asmsStr = File.OpenRead(Game.GameFiles + "/" + game["Assembly-Package-File"]);
            });
        }
        else
            progress = max;
        while (progress != 0)
            yield return new WaitForSeconds(0.1f);
        FadeOutProgressBarValue();
        while (!ready)
            yield return new WaitForSeconds(0.1f);

        // Read
        DataReader assemblies = new DataReader(new GZipStream(asmsStr, CompressionMode.Decompress));

        // Load assemblies into memory
        int count = assemblies.ReadInt();
        max = count;
        Debug.Log("Loading " + count + " assembly file(s)...");
        StartCoroutine(ProgressBar());
        progress = 1;
        AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
        {
            string file = args.Name;
            if (args.Name.StartsWith(Game.GameID + "-"))
                file = args.Name.Substring(Game.GameID.Length + 1);
            if (File.Exists(Game.GameFiles + "/" + file + ".dll"))
            {
                return Assembly.LoadFrom(Game.GameFiles + "/" + file + ".dll");
            }
            return null;
        };
        for (int i = 0; i < count; i++)
        {
            progress = i + 1;
            string name = assemblies.ReadString();
            try
            {
                // Load definition
                File.WriteAllBytes(Game.GameFiles + "/" + name, assemblies.ReadBytes());
                using (Stream strm = File.Open(Game.GameFiles + "/" + name, FileMode.Open, FileAccess.ReadWrite))
                {
                    AssemblyDefinition def = AssemblyDefinition.ReadAssembly(strm);

                    // Modify it
                    def.Name.Name = Path.GetFileNameWithoutExtension(name);
                    def.MainModule.Name = Path.GetFileNameWithoutExtension(name);

                    // Build and save to disk
                    def.Write();
                    strm.Close();
                }
                Assembly asmbl = Assembly.Load(Game.GameID + "-" + Path.GetFileNameWithoutExtension(name));
                Debug.Log("Loaded assembly: " + name + ": " + asmbl.GetTypes().Length + ", type(s) loaded.");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load assembly: " + name);
                Debug.LogError(e);
                // Error screen
                ErrorWindow("Failed to load primary assemply package.\nUnable to start the game.\n\nError: failed to load assembly: " + name + ".\n\nGame: " + Game.Title + "\nVersion: " + Game.Version);
                yield break;
            }
        }
        assemblies.GetStream().Close();
        while (progress != 0)
            yield return new WaitForSeconds(0.1f);
        ready = false;
        FadeOutProgressBar();
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        ready = false;
        FadeIn();
        yield return new WaitForSeconds(1.4f);
        ready = false;

        // Download prestart package
        url = game["Prestart-Package-File"];
        if (!url.StartsWith("file:") && !url.StartsWith("http:") && !url.StartsWith("https:"))
        {
            if (!url.StartsWith("/"))
                url = "/" + url;
            url = game["Data-Download-Base-Url"] + url;
        }
        url = url.Replace("$<platform>", Environment.OSVersion.Platform == PlatformID.Unix ? "linux64" : "win64");
        Debug.Log("Downloading asset package...");
        Stream assetsStr = Utils.DownloadAssets(url);
        if (assetsStr == null)
        {
            Debug.LogWarning("Download rejected.");

            // We are offline
            game["Session"] = "OFFLINE";

            // Find cached
            if (File.Exists(Game.GameFiles + "/" + game["Prestart-Package-File"]))
            {
                Debug.Log("Loading cached asset package...");
                assetsStr = File.OpenRead(Game.GameFiles + "/" + game["Prestart-Package-File"]);
            }

            // Contact refresh endpoint
            newToken = Utils.DownloadString(game["Refresh-Endpoint"], "GET", null, new Dictionary<string, string>()
            {
                ["Authorization"] = "Bearer " + sessionToken
            });

            if (newToken != null && newToken != "")
                game["Session"] = newToken;
            else
            {
                Debug.LogWarning("Entering offline mode...");
                if (!Game.OfflineSupport)
                {
                    // Error screen
                    Debug.LogError("Entering offline mode failed: no offline play support.");
                    ErrorWindow("Please connect your device to the internet before continuing.");
                    yield break;
                }
            }

            if (assetsStr == null)
            {
                Debug.LogError("Could not download or load a cached assembly package.");

                // Error screen
                ErrorWindow("Failed to download primary assets package.\nUnable to start the game.\n\nError: server rejected download request.\nA connection error is likely the cause.\n\nGame: " + Game.Title + "\nVersion: " + Game.Version);
                yield break;
            }

            Game.Init(game);
        }

        // Download
        wait = true;
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        FadeInProgressBar();
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        max = 100;
        StartCoroutine(ProgressBar());
        progress = 1;
        if (!(assetsStr is FileStream))
        {
            yield return Task.Run(() =>
            {
                Stream outP = File.OpenWrite(Game.GameFiles + "/" + game["Prestart-Package-File"]);
                long l = assetsStr.Length;
                max = l;
                for (long i = 0; i < l; i++)
                {
                    progress = i + 1;
                    outP.WriteByte((byte)assetsStr.ReadByte());
                }
                progress = max;
                assetsStr.Close();
                outP.Close();
                assetsStr = File.OpenRead(Game.GameFiles + "/" + game["Prestart-Package-File"]);
            });
        }
        else
            progress = max;
        while (progress != 0)
            yield return new WaitForSeconds(0.1f);
        FadeOutProgressBar();
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        ready = false;
        FadeIn();

        // Protect the seal and product key
        Game.Clean();

        List<string> assetFiles = new List<string>();
        DataReader assets = new DataReader(new GZipStream(assetsStr, CompressionMode.Decompress));
        int c = assets.ReadInt();
        Debug.Log("Extracting " + c + " asset file(s)...");
        yield return Task.Run(() =>
        {
            for (int i = 0; i < c; i++)
            {
                string name = assets.ReadString();
                Debug.Log("Extracting: " + name + "...");
                string fileName = Game.AssetsFolder + "/core/" + name;
                string dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                Stream outP = File.OpenWrite(fileName);
                assetFiles.Add(fileName);
                outP.Write(assets.ReadBytes());
                outP.Close();
            }
            assets.GetStream().Close();
        });
        while (c != assetFiles.Count)
            yield return new WaitForSeconds(0.1f);

        // If it exists, load image
        string image = Game.AssetsFolder + "/core/logo.png";
        if (File.Exists(image))
        {
            Texture2D t = new Texture2D(100, 100);
            t.LoadImage(File.ReadAllBytes(image));
            GameObject.Find("/Canvas/Panel/GameLogo").GetComponent<RawImage>().texture = t;
        }

        // Load assets
        wait = true;
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        FadeInProgressBar();
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        ready = true;
        FadeIn();
        yield return new WaitForSeconds(2f);
        progress = 0;
        max = assetFiles.Count;
        Debug.Log("Loading " + max + " asset file(s)...");
        StartCoroutine(ProgressBar());
        progress = 1;
        foreach (string asset in assetFiles)
        {
            if (!asset.EndsWith(".pxi") && !asset.EndsWith(".png") && !asset.EndsWith(".jpg"))
            {
                Debug.Log("Loading asset: " + Path.GetFileName(asset) + "...");
                var a = AssetBundle.LoadFromFileAsync(asset);
                float last = 0;
                while (!a.isDone)
                {
                    if (a.progress != last)
                    {
                        progress += (a.progress - last);
                        last = a.progress;
                    }
                    yield return new WaitForSeconds(0.1f);
                }
            } else
                progress++;
        }
        FadeInGameLogo();
        progress = assetFiles.Count;
        while (progress != 0)
            yield return new WaitForSeconds(0.1f);
        yield return new WaitForSeconds(0.5f);

        // Fade out everything
        ready = false;
        ComepleteFadeOut();
        while (!ready)
            yield return new WaitForSeconds(0.1f);

        // Load game properties
        Dictionary<string, string> conf = new Dictionary<string, string>();
        conf["Scene"] = "Bootstrapper";
        if (File.Exists(Game.AssetsFolder + "/core/game.pxi"))
        {
            foreach (string line in File.ReadAllLines(Game.AssetsFolder + "/core/game.pxi"))
            {
                if (line == "" || !line.Contains(": "))
                    continue;
                string key = line.Remove(line.IndexOf(": "));
                string val = line.Substring(line.IndexOf(": ") + 2);
                conf[key] = val;
            }
        }
        if (conf.ContainsKey("Module"))
        {
            // Load module
            Debug.Log("Loading module: " + conf["Module"] + "...");
            try
            {
                Type mod = FindType(conf["Module"]);
                object module = mod.GetConstructor(new Type[0]).Invoke(new object[0]);
                module.GetType().GetMethod("Start", new Type[0]).Invoke(module, new object[0]);
                module.GetType().GetMethod("LoadMainScene", new Type[0]).Invoke(module, new object[0]);
            }
            catch (Exception e)
            {
                // Error screen
                Debug.LogError("Failed to load module: " + conf["Module"]);
                Debug.LogError(e);
                ErrorWindow("Failed to load primary game module.\nUnable to start the game.\n\nError: " + e.Message + ".\n\nGame: " + Game.Title + "\nVersion: " + Game.Version);
                yield break;
            }
        }
        else 
        {
            // Load scene
            Debug.Log("Loading scene: " + conf["Scene"]);
            SceneManager.LoadScene(conf["Scene"]);
        }
    }

    private Type FindType(string name)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name == name)
                        return t;
                }
            } 
            catch
            {

            }
        }
        return null;
    }

    IEnumerator ProgressBar()
    {
        float last = progress;
        while (last < max)
        {
            yield return new WaitForSeconds(0.1f);
            if (last == progress)
                continue;
            SetProgressBarValue(progress, max);
            while (!ready)
                yield return new WaitForSeconds(0.1f);
            last = progress;
        }
        SetProgressBarValue(1, 1);
        while (!ready)
            yield return new WaitForSeconds(0.1f);
        progress = 0;
        yield return null;
    }

    IEnumerator WaitBeforeFade(float delay)
    {
        yield return new WaitForSeconds(delay);
        FadeIn();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
