using System.IO;
using UnityEngine;
using YooAsset.Editor;

[DisplayName("只收集预制件,排除#开头的文件")]
public class CollectPrefabgnoring : IAssetFilterRule
{
    public string FindAssetType
    {
        get { return EAssetFilterType.Prefab.ToString(); }
    }

    public bool IsCollectAsset(AssetFilterRuleData data)
    {
        var path = data.AssetPath;
        return Path.GetExtension(path) == ".prefab" && !path.Contains("#");
    }
}
