using System.IO;
using YooAsset.Editor;

[DisplayName("只收集shader,排除#开头的文件")]
public class CollectShaderIgnoring : IAssetFilterRule
{
    public string FindAssetType
    {
        get { return EAssetFilterType.Shader.ToString(); }
    }

    public bool IsCollectAsset(AssetFilterRuleData data)
    {
        var path = data.AssetPath;
        return Path.GetExtension(data.AssetPath) == ".shader" && !path.Contains("#");
    }
}
