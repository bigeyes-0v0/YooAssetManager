using System.IO;
using System.Runtime.CompilerServices;
using YooAsset.Editor;

[DisplayName("定位地址：文件名.后缀")]
public class AddressByFileNameType : IAddressRule
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string IAddressRule.GetAssetAddress(AddressRuleData data)
    {
        return Path.GetFileName(data.AssetPath);
    }
}
