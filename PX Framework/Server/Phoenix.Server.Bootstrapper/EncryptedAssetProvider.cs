using Phoenix.Server.Bootstrapper.Packages;
using System.Text;

namespace Phoenix.Server.Bootstrapper
{
    public class EncryptedAssetProvider : IAssetProvider
    {
        private BinaryPackage launchPackage;

        public EncryptedAssetProvider(BinaryPackage launchPackage)
        {
            this.launchPackage = launchPackage;
        }

        public string[] GetAssetsIn(string folder)
        {
            // Find assets
            BinaryPackageEntry[] entries = launchPackage.GetEntriesIn("Assets/" + folder);
            return entries.Select(t => t.Key.Substring(t.Key.LastIndexOf("/") + 1)).ToArray();
        }

        public Stream? GetAssetStream(string asset)
        {
            // Find asset ID
            BinaryPackageEntry? idEntry = launchPackage.GetEntry("Assets/" + asset);
            if (idEntry == null)
                return null;
            string assetID = Encoding.UTF8.GetString(Program.ReadBytesFromEntry(idEntry, launchPackage));

            // Decrypt asset
            Stream data = File.OpenRead("Assets/" + assetID + ".epaf");
            Stream dec = Program.Decrypt(data, assetID, launchPackage);
            DataReader rd = new DataReader(dec);
            byte[] data2 = rd.ReadAllBytes();
            dec.Close();
            return new MemoryStream(data2);
        }
    }
}