using System.IO;

namespace PlaylandBoxer;

public class GameAssets
{
    private const string AssetFolder = "Assets";

    public bool Load()
    {
        if (!Directory.Exists(AssetFolder))
        {
            Directory.CreateDirectory(AssetFolder);
            return false;
        }

        var files = Directory.GetFiles(AssetFolder);
        return files.Length > 0;
    }
}
