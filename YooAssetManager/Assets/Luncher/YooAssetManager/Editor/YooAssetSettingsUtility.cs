using UnityEngine;
using UnityEditor;
using YooAsset.Editor;
using System;
using System.IO;
using System.Collections.Generic;
using Unity.Collections;
public class YooAssetSettingsUtility
{
    public static void Refresh()
    {
        var setting = LoadSettingData();
        var dict = GetAssetBundleCollectorSettingDict(setting);

        string[] assetPaths = AssetDatabase.FindAssets("p: a:Assets t:DefaultAsset");
        var length = assetPaths.Length;
        for (var i = 0; i < length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetPaths[i]);
            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);
            if ((!isFile && isDirectory) == false) continue;
            string name = Path.GetFileNameWithoutExtension(path);
            var has = name.StartsWith("$");
            if (has)
            {
                AddBundleFolder(dict, path, name);
            }
        }
        SetAssetBundleCollectorSettingDict(setting, dict);
        ClearEmptyBundleFolder(setting);
        SaveSetting(setting);
    }

    static void RemoveBundleFolder(string folderPath)
    {
        var setting = LoadSettingData();
        var bundleData = FindBundleData(setting, folderPath);
        if (bundleData.Package == null)
        {
            return;
        }
        bundleData.group.Collectors.Remove(bundleData.collector);
    }

    static void AddBundleFolder(string[] importedAsset, string[] movedAssets)
    {
        var setting = LoadSettingData();
        var dict = GetAssetBundleCollectorSettingDict(setting);
        var length = importedAsset.Length;
        int count = 0;
        for (var i = 0; i < length; i++)
        {
            var path = importedAsset[i];
            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);
            if (!isFile && isDirectory)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var has = name.StartsWith("$");
                if (has)
                {
                    AddBundleFolder(dict, path, name);
                    count++;
                }
            }
        }

        length = movedAssets.Length;
        count = 0;
        for (var i = 0; i < length; i++)
        {
            var path = movedAssets[i];
            var isFile = File.Exists(path);
            var isDirectory = Directory.Exists(path);
            if (!isFile && isDirectory)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var has = name.StartsWith("$");
                if (has)
                {
                    AddBundleFolder(dict, path, name);
                    count++;
                }
            }
        }
        SetAssetBundleCollectorSettingDict(setting, dict);
        ClearEmptyBundleFolder(setting);
        SaveSetting(setting);
    }

    static void ClearEmptyBundleFolder(BundleCollectorSetting setting)
    {
        var mainPackage = setting.Packages[0];
        var group = mainPackage.Groups[0];
        var removeList = new List<BundleCollector>();
        foreach (BundleCollector collector in group.Collectors)
        {
            var guid = AssetDatabase.GUIDFromAssetPath(collector.CollectPath);
            if (guid.Empty())
            {
                removeList.Add(collector);
            }
            else
            {
                string name = Path.GetFileNameWithoutExtension(collector.CollectPath);
                var has = name.StartsWith("$");
                if (!has)
                {
                    removeList.Add(collector);
                }
            }
        }
        foreach (var item in removeList)
        {
            group.Collectors.Remove(item);
        }
    }

    private static void SaveSetting(BundleCollectorSetting setting)
    {
        EditorUtility.SetDirty(setting);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void AddBundleFolder(
        Dictionary<string, BundleCollector> dict,
        string folderPath,
        string name)
    {
        var lowerPath = folderPath.ToLower();
        if (!dict.TryGetValue(lowerPath, out var collector))
        {
            collector = CreateNewCollector(folderPath);
            dict.Add(folderPath, collector);
        }
        var folderGUID = AssetDatabase.AssetPathToGUID(folderPath);
        collector.CollectPath = folderPath;
        collector.CollectorGUID = folderGUID;
        var collectorType = ECollectorType.MainAssetCollector;
        if (name.Length > 2)
        {
            var folderNameSpan = name.AsSpan();
            int secondDollar = folderNameSpan.Slice(1).IndexOf('$');
            // 只有一个 $，无过滤规则，收集所有资源
            if (secondDollar >= 0)
            {
                var c = folderNameSpan[1];
                switch (c)
                {
                    case '1': collectorType = ECollectorType.StaticAssetCollector; break;
                    case '2': collectorType = ECollectorType.DependAssetCollector; break;
                }
            }
        }
        collector.CollectorType = collectorType;
        collector.AddressRuleName = "AddressByFileNameType";
        collector.PackRuleName = "PackCollector";
        collector.FilterRuleName = "CollectAllIgnoring";
    }

    private static BundleCollector CreateNewCollector(string folderPath)
    {
        var folderGUID = AssetDatabase.AssetPathToGUID(folderPath);
        BundleCollector newCollector = new BundleCollector()
        {
            CollectPath = folderPath,
            CollectorGUID = folderGUID,
            CollectorType = ECollectorType.MainAssetCollector,
            AddressRuleName = "AddressByFileNameType",
            PackRuleName = "PackCollector",
            FilterRuleName = "CollectAllIgnoring",
        };
        return newCollector;
    }

    private static BundleData FindBundleData(BundleCollectorSetting setting, string folderGUID)
    {
        var mainPackage = setting.Packages[0];
        var group = mainPackage.Groups[0];
        foreach (BundleCollector collector in group.Collectors)
        {
            if (collector.CollectorGUID == folderGUID)
            {
                return new BundleData() { Package = mainPackage, group = group, collector = collector };
            }
        }

        return default;
    }

    private static Dictionary<string, BundleCollector> GetAssetBundleCollectorSettingDict(BundleCollectorSetting setting)
    {
        var collectors = setting.Packages[0].Groups[0].Collectors;
        Dictionary<string, BundleCollector> dict = new Dictionary<string, BundleCollector>(collectors.Count);
        List<BundleCollector> removeList = new();
        foreach (BundleCollector collector in collectors)
        {
            var path = collector.CollectPath.ToLower(); // 转为小写
            var guid = AssetDatabase.AssetPathToGUID(collector.CollectPath, AssetPathToGUIDOptions.OnlyExistingAssets);
            // 检查是否存在文件 
            if (string.IsNullOrEmpty(guid))
            {
                removeList.Add(collector);
            }
            else
            {
                //存在的话检查是否重复添加
                var success = dict.TryAdd(path, collector);
                if (!success)
                {
                    removeList.Add(collector);
                }
            }
        }
        foreach (var item in removeList)
        {
            collectors.RemoveSwapBack(item);
        }
        return dict;
    }

    private static void SetAssetBundleCollectorSettingDict(BundleCollectorSetting setting, Dictionary<string, BundleCollector> dict)
    {
        var collectors = setting.Packages[0].Groups[0].Collectors;
        collectors.Clear();
        collectors.Capacity = dict.Count;
        foreach (var item in dict.Values)
        {
            collectors.Add(item);
        }
    }

    static BundleCollectorSetting LoadSettingData()
    {
        var setting = BundleCollectorSettingData.Setting;
        var package = new BundleCollectorPackage() { PackageName = "MainPackage" };
        package.Groups.Add(new BundleCollectorGroup() { GroupName = "MainGroup" });
        setting.Packages.Clear();
        setting.Packages.Add(package);
        package.EnableAddressable = true;
        return setting;
    }

    struct BundleData
    {
        public BundleCollectorPackage Package;
        public BundleCollectorGroup group;
        public BundleCollector collector;
    }
}
