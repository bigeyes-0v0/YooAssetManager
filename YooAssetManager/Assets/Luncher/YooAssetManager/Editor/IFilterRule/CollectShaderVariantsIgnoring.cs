using System.IO;
using YooAsset.Editor;

[DisplayName("只收集shader变体,排除#开头的文件")]
public class CollectShaderVariantsIgnoring : IAssetFilterRule
{
    public string FindAssetType
    {
        get { return EAssetFilterType.Shader.ToString(); }
    }

    public bool IsCollectAsset(AssetFilterRuleData data)
    {
        return Path.GetExtension(data.AssetPath) == ".shadervariants" && !data.AssetPath.Contains("#");
    } 
}
