using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


public class ScriptImportPostProcessor : AssetPostprocessor
{
    public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var targets = importedAssets.Where(path => System.IO.Path.GetExtension(path) == ".cs");
        targets.Concat(movedAssets.Where(path => System.IO.Path.GetExtension(path) == ".cs"));
        foreach (var path in targets)
        {
            var fullPath = path.Replace("Assets", Application.dataPath);
            var encoding = EncodeHelper.GetJpEncoding(path);
            if (encoding == null)
            {
                Debug.LogError("Failed to get encoding");
                continue;
            }
            if (encoding.EncodingName == Encoding.UTF8.EncodingName)
            {
                continue;
            }
            var data = string.Empty;
            using (var sr = new StreamReader(path, encoding))
            {
                data = sr.ReadToEnd();
            }
            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                sw.Write(data);
            }
            Debug.Log($"{path} is encoded {encoding.EncodingName} to UTF8");
        }
    }
}
