using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset.Editor;
using static SpriteAtlasData;

/// <summary>
/// 图集sprite收集顺序SpriteAtlas->MultipleTexture2D->SingleTexture2D
/// sprite名称不能重复
/// </summary>
public class SpriteAtlasUtility
{
    private const string SpriteAtlasDataPath = "Assets/$SpriteAtlasData/" + SpriteAtlasManager.SpriteAtlasDataName;

    [MenuItem("YooAsset/RefreshSpriteAtlas")]
    public static void Refresh()
    {
        var bundleCollectorSetting = BundleCollectorSettingData.Setting;
        var packages = bundleCollectorSetting.Packages;
        CollectResult[] collectResults = new CollectResult[packages.Count];
        for (var i = 0; i < packages.Count; i++)
        {
            collectResults[i] = bundleCollectorSetting.BeginCollect(packages[i].PackageName, true, true);
        }

        //收集在资源清单里的精灵图
        List<string> oneSpriteTexture2DList = new(512);
        List<string> multipleSpriteTexture2DList = new(512);
        List<string> spriteAtlasList = new(512);
        foreach (var collectResult in collectResults)
        {
            foreach (var item in collectResult.CollectAssets)
            {
                if (item.CollectorType != ECollectorType.MainAssetCollector)
                    continue;
                var assetPath = item.AssetInfo.AssetPath;
                if (item.AssetInfo.AssetType == typeof(Texture2D))
                {
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null || importer.textureType != TextureImporterType.Sprite)
                        continue;
                    if (importer.spriteImportMode == SpriteImportMode.Multiple)
                    {
                        multipleSpriteTexture2DList.Add(assetPath);
                    }
                    else
                    {
                        oneSpriteTexture2DList.Add(assetPath);
                    }
                }
                else if (item.AssetInfo.AssetType == typeof(SpriteAtlas))
                {
                    spriteAtlasList.Add(assetPath);
                }
            }
        }

        Dictionary<Sprite, string> spriteWarningDict = new(oneSpriteTexture2DList.Count + multipleSpriteTexture2DList.Count + spriteAtlasList.Count);
        Dictionary<Sprite, int> onlyOneSpriteCheckDict = new(oneSpriteTexture2DList.Count + multipleSpriteTexture2DList.Count + spriteAtlasList.Count);
        Dictionary<string, List<string>> atlas2SpriteNameListDict = new(spriteAtlasList.Count);
        Dictionary<string, List<string>> multipleSprite2SpriteNameListDict = new(multipleSpriteTexture2DList.Count);
        List<string> oneSpriteFileNameList = new(oneSpriteTexture2DList.Count);


        foreach (var assetPath in spriteAtlasList)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);
            string fileName = Path.GetFileName(assetPath);
            var packables = atlas.GetPackables();
            List<string> spriteList = null;
            foreach (Object obj in packables)
            {
                if (obj is Texture2D texture2D)
                {
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture2D));
                    foreach (var item in allAssets)
                    {
                        if (item is Sprite sprite && Check(sprite, assetPath, spriteWarningDict, onlyOneSpriteCheckDict))
                        {
                            if (spriteList == null)
                            {
                                spriteList = new(packables.Length);
                                atlas2SpriteNameListDict.Add(fileName, spriteList);
                            }
                            spriteList.Add(sprite.name);
                        }
                    }
                }
            }
        }

        foreach (var assetPath in multipleSpriteTexture2DList)
        {
            string fileName = Path.GetFileName(assetPath);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<string> spriteList = null;
            for (int i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] is Sprite sprite && Check(sprite, assetPath, spriteWarningDict, onlyOneSpriteCheckDict))
                {
                    if (spriteList == null)
                    {
                        spriteList = new(allAssets.Length);
                        multipleSprite2SpriteNameListDict.Add(fileName, spriteList);
                    }
                    spriteList.Add(sprite.name);
                }
            }
        }

        foreach (var assetPath in oneSpriteTexture2DList)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (Check(sprite, assetPath, spriteWarningDict, onlyOneSpriteCheckDict))
            {
                oneSpriteFileNameList.Add(Path.GetFileName(assetPath));
            }
        }
        CheckWarning(spriteWarningDict, onlyOneSpriteCheckDict);
        InitEditor(atlas2SpriteNameListDict, multipleSprite2SpriteNameListDict, oneSpriteFileNameList);
    }

    static void InitEditor(
        Dictionary<string, List<string>> atlas2SpriteNameListDict,
        Dictionary<string, List<string>> multipleSprite2SpriteNameListDict,
        List<string> oneSpriteFileNameList
        )
    {

        var spriteAtlasV2SaveDatas = new SaveData[atlas2SpriteNameListDict.Count];
        var multipleTexture2DSaveDatas = new SaveData[multipleSprite2SpriteNameListDict.Count];

        var index = 0;
        //排序是为了降低修改差异
        foreach (var kvp in atlas2SpriteNameListDict.OrderBy(kvp => kvp.Key))
        {
            var list = kvp.Value;
            list.Sort();
            spriteAtlasV2SaveDatas[index] = new(kvp.Key, list.ToArray());
            index++;
        }

        index = 0;
        foreach (var kvp in multipleSprite2SpriteNameListDict.OrderBy(kvp => kvp.Key))
        {
            var list = kvp.Value;
            list.Sort();
            multipleTexture2DSaveDatas[index] = new(kvp.Key, list.ToArray());
        }
        oneSpriteFileNameList.Sort();

        var spriteAtlasData = LoadSpriteAtlasData();
        spriteAtlasData.spriteAtlasSaveDatas = spriteAtlasV2SaveDatas;
        spriteAtlasData.multipleTexture2DSaveDatas = multipleTexture2DSaveDatas;
        spriteAtlasData.singleSpritefileNames = oneSpriteFileNameList.ToArray();

        EditorUtility.SetDirty(spriteAtlasData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static bool Check(
        Sprite sprite,
        string assetPath,
        Dictionary<Sprite, string> spriteWarningDict,
        Dictionary<Sprite, int> onlyOneSpriteCheckDict
        )
    {
        if (sprite == null)
            return false;
        if (!onlyOneSpriteCheckDict.TryGetValue(sprite, out var count))
        {
            spriteWarningDict.Add(sprite, assetPath);
            onlyOneSpriteCheckDict.Add(sprite, 1);
            return true;
        }
        count++;
        onlyOneSpriteCheckDict[sprite] = count;

        var oldName = spriteWarningDict[sprite];
        spriteWarningDict[sprite] = $"{oldName}\n{assetPath}";
        return false;
    }

    static void CheckWarning(
        Dictionary<Sprite, string> spriteWarningDict,
        Dictionary<Sprite, int> onlyOneSpriteCheckDict
        )
    {
        foreach (var item in onlyOneSpriteCheckDict)
        {
            var sprite = item.Key;
            var count = item.Value;
            if (count > 1)
            {
                Debug.LogWarning($"精灵图：{sprite.name}出现重复收集\n{spriteWarningDict[sprite]}");
            }
        }
    }

    public static SpriteAtlasData LoadSpriteAtlasData()
    {
        var setting = AssetDatabase.LoadAssetAtPath<SpriteAtlasData>(SpriteAtlasDataPath);
        if (!setting)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SpriteAtlasDataPath));
            setting = ScriptableObject.CreateInstance<SpriteAtlasData>();
            AssetDatabase.CreateAsset(setting, SpriteAtlasDataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        return setting;
    }

}
