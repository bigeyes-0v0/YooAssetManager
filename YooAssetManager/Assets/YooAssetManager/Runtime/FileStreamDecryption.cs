using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using YooAsset;

/// <summary>
/// 资源文件流加载解密类
/// </summary>
public class FileStreamDecryption : IBundleStreamDecryptor
{
 
    public int GetBufferSize(BundleDecryptArgs args)
    {
        return 1024;
    }

    public Stream CreateDecryptionStream(BundleDecryptArgs args)
    {
        BundleStream bundleStream = new(args.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return bundleStream;
    }
}