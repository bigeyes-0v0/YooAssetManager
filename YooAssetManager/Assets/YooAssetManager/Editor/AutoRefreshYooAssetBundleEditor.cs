using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

public class AutoRefreshYooAssetBundleEditor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAsset, string[] deleteAsset, string[] movedAssets, string[] movedFromAssetPaths)
    {
        //这是因为在unity6 和2022的资源变动的回调有差异，增量读写可能会遗漏,采用全量刷新
        // YooAssetSettingsUtility.AddBundleFolder(importedAsset, movedAssets);
        YooAssetSettingsUtility.Refresh();
    }
    
    [InitializeOnLoadMethod]
    static void InitializeOnLoad()
    {
        EditorApplication.delayCall += Initialize;
    }

    [MenuItem("YooAsset/Refresh")]
    public static void Initialize()
    {
        YooAssetSettingsUtility.Refresh();
        SpriteAtlasUtility.Refresh();
    }
}
