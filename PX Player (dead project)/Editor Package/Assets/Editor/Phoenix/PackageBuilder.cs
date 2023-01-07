using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO.Compression;
using Phoenix.Editor.IO;
using System.Text;
using Phoenix.Editor;

public class PackageBuilder
{
	[MenuItem("Phoenix/Build Packages")]
	static void BuildAllAssetBundles()
	{
		Directory.CreateDirectory("Build/px/packages");

		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
			Directory.CreateDirectory("Build/px/assets/win64");
		else
			Debug.LogWarning("No support for Win64 builds");
		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
			Directory.CreateDirectory("Build/px/assets/linux64");
		else
			Debug.LogWarning("No support for Linux64 builds");
		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
			Directory.CreateDirectory("Build/px/assets/osx");
		else
			Debug.LogWarning("No support for OSX builds");

		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
			BuildAssets("Build/px/assets/win64", BuildTarget.StandaloneWindows64);
		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
			BuildAssets("Build/px/assets/linux64", BuildTarget.StandaloneWindows64);
		if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
			BuildAssets("Build/px/assets/osx", BuildTarget.StandaloneWindows64);


		// Build assembly package
		Debug.Log("");
		List<FileInfo> files = new List<FileInfo>();
		files.Add(new FileInfo("Library/ScriptAssemblies/Assembly-CSharp.dll"));
		FileStream strmU = File.OpenWrite("Build/px/packages/assemblies.pxpkg");
		GZipStream strm = new GZipStream(strmU, CompressionMode.Compress);
		DataWriter writer = new DataWriter(strm);
		writer.WriteInt(files.Count);
		foreach (FileInfo file in files)
		{
			writer.WriteString(file.Name == "Assembly-CSharp.dll" ? "PXGAME-ASSEMBLY.dll" : file.Name);
			writer.WriteBytes(File.ReadAllBytes(file.FullName));
		}
		strm.Close();
		strmU.Close();
	}

	private static void BuildAssets(string output, BuildTarget target)
    {
		// Log
		Debug.Log("Building asset bundles for " + target.ToString() + "...");

		// Find files
		Object[] objects = Resources.FindObjectsOfTypeAll(typeof(Object));
		string[] scenes = new string[EditorBuildSettings.scenes.Length];
		for (int i = 0; i < scenes.Length; i++)
		{
			scenes[i] = EditorBuildSettings.scenes[i].path;
		}

		// Build bundle
		AssetBundleBuild build = new AssetBundleBuild();
		build.assetBundleName = "core.assets";
		build.assetNames = scenes;
		RefactoringBundleBuilder.Build(output, new AssetBundleBuild[] { build }, target);

		// Build asset package
		Debug.Log("Building asset package prestart-" + Path.GetFileName(output) + ".pxpkg...");
		List<FileInfo> files = new List<FileInfo>();
		files.Add(new FileInfo(output + "/core.assets"));
		files.AddRange(new DirectoryInfo("Assets/Editor/Phoenix/Include").GetFiles().Where(t => !t.Name.EndsWith(".meta")));
		FileStream strmU = File.OpenWrite(output + "/../../packages/prestart-" + Path.GetFileName(output) + ".pxpkg");
		GZipStream strm = new GZipStream(strmU, System.IO.Compression.CompressionLevel.Optimal);
		DataWriter writer = new DataWriter(strm);
		writer.WriteInt(files.Count);
		foreach (FileInfo file in files)
        {
			writer.WriteString(file.Name);
			writer.WriteBytes(File.ReadAllBytes(file.FullName));
		}
		strm.Close();
		strmU.Close();
	}
}
