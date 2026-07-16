using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

[DisplayName("收集所有资源并且进行匹配过滤,排除#开头的文件")]
public class CollectAllIgnoring : IAssetFilterRule
{
    public string FindAssetType
    {
        get { return EAssetFilterType.All.ToString(); }
    }

    public bool IsCollectAsset(AssetFilterRuleData data)
    {
        return CheckCollectAsset(data);
    }

    private static ReadOnlySpan<char> GetFolderName(ReadOnlySpan<char> path)
    {
        // 去掉尾部路径分隔符
        int len = path.Length;
        while (len > 0 && (path[len - 1] == '/' || path[len - 1] == '\\'))
        {
            len--;
        }
        path = path.Slice(0, len);

        // 找最后一个路径分隔符
        for (int i = len - 1; i >= 0; i--)
        {
            if (path[i] == '/' || path[i] == '\\')
            {
                return path.Slice(i + 1);
            }
        }
        return path;
    }

    private static ReadOnlySpan<char> GetExtension(ReadOnlySpan<char> path)
    {
        for (int i = path.Length - 1; i >= 0; i--)
        {
            if (path[i] == '.')
            {
                return path.Slice(i + 1);
            }
            if (path[i] == '/' || path[i] == '\\')
            {
                break;
            }
        }
        return ReadOnlySpan<char>.Empty;
    }

    private static bool ExtEqualsIgnoreCase(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (char.ToLowerInvariant(a[i]) != char.ToLowerInvariant(b[i]))
                return false;
        }
        return true;
    }


    public static bool CheckCollectAsset(AssetFilterRuleData data)
    {
        var assetPath = data.AssetPath;
        // 排除路径中包含#的文件或文件夹
        if (assetPath.Contains("#"))
        {
            return false;
        }

        // 解析收集文件夹名称中的扩展名过滤规则
        // 文件夹名格式: $[1|2][@ext|@!ext]*$自定义名称 或 $自定义名称
        ReadOnlySpan<char> folderName = GetFolderName(data.CollectPath.AsSpan());

        if (folderName.IsEmpty || folderName[0] != '$')
        {
            return true;
        }

        // 找第二个 $
        int secondDollar = folderName.Slice(1).IndexOf('$');
        if (secondDollar < 0)
        {
            return true;
        }
        secondDollar += 1;

        // $$ 之间的内容范围 [start, end)
        int start = 1;
        int end = secondDollar;

        // 跳过可选的 1 或 2 前缀
        if (end > start && (folderName[start] == '1' || folderName[start] == '2'))
        {
            start++;
        }

        // 内容切片
        ReadOnlySpan<char> content = folderName.Slice(start, end - start);

        // 没有 @ 过滤规则，收集所有资源
        if (content.IndexOf('@') < 0)
        {
            return true;
        }

        // 获取资源扩展名（不含点）
        ReadOnlySpan<char> assetExt = GetExtension(assetPath.AsSpan());

        // 逐字符解析 @ext（白名单）和 @!ext（黑名单）
        bool hasWhitelist = false;
        bool inWhitelist = false;

        int pos = 0;
        while (pos < content.Length)
        {
            if (content[pos] == '@')
            {
                pos++;
                continue;
            }

            bool isBlacklist = false;
            if (pos < content.Length && content[pos] == '!')
            {
                isBlacklist = true;
                pos++;
            }

            int extStart = pos;
            while (pos < content.Length && content[pos] != '@')
            {
                pos++;
            }

            if (extStart >= pos)
            {
                continue;
            }

            bool match = ExtEqualsIgnoreCase(content.Slice(extStart, pos - extStart), assetExt);

            if (isBlacklist)
            {
                if (match)
                    return false;
            }
            else
            {
                hasWhitelist = true;
                if (match)
                    inWhitelist = true;
            }
        }

        if (hasWhitelist && !inWhitelist)
        {
            return false;
        }

        return true;
    }
}
