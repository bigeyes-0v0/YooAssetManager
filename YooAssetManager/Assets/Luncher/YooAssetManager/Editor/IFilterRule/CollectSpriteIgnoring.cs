using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

[DisplayName("只收集精灵图,排除#开头的文件")]
public class CollectSpriteIgnoring : IAssetFilterRule
{
    public string FindAssetType
    {
        get { return EAssetFilterType.Sprite.ToString(); }
    }

    public bool IsCollectAsset(AssetFilterRuleData data)
    {
        var path = data.AssetPath;
        if (path.Contains("#"))
        {
            return false;
        }
        if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null && textureImporter.textureType == TextureImporterType.Sprite)
            {
                return true;
            }

            return false;
        }

        return false;
    }
}
