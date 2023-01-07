using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Phoenix.Editor
{
	/// <summary>
	/// This tool changes the Assembly-CSharp file name to PXGAME-ASSEMBLY while building asset bundles.
	/// Note: asset compression is unavailable.
	/// </summary>
	public static class RefactoringBundleBuilder
	{
		/// <summary>
		/// Builds and refactors a asset bundle
		/// </summary>
		/// <param name="output">Output folder</param>
		/// <param name="builds">Asset bundle build information</param>
		public static void Build(string output, AssetBundleBuild[] builds, BuildTarget target)
		{
			// Build bundles
			BuildPipeline.BuildAssetBundles(output, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.UncompressedAssetBundle, target);

			foreach (AssetBundleBuild build in builds)
            {
				// Refactor
				byte[] data = File.ReadAllBytes(output + "/" + build.assetBundleName);
				data = ReplaceBytes(data, Encoding.UTF8.GetBytes("Assembly-CSharp.dll"), Encoding.UTF8.GetBytes("PXGAME-ASSEMBLY.dll"));
				File.WriteAllBytes(output + "/" + build.assetBundleName, data);
			}
		}

		private static byte[] ReplaceBytes(byte[] array, byte[] target, byte[] replacement)
		{
			MemoryStream output = new MemoryStream();
			List<byte> buffer = new List<byte>();
			for (int i = 0; i < array.Length; i++)
            {
				int pos = buffer.Count;
				byte b = array[i];
				if (pos < target.Length && b == target[pos])
                {
					buffer.Add(b);
                }
				else if (pos == target.Length)
                {
					// Perform replacement
					output.Write(replacement);
					buffer.Clear();
					output.WriteByte(b);
				}
				else
                {
					if (pos != 0)
					{
						output.Write(buffer.ToArray());
						buffer.Clear();
					}
					output.WriteByte(b);
                }
            }
			return output.ToArray();
		}
	}
}