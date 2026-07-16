using System;
using System.IO;
using UnityEngine;
using YooAsset;

public class FileStreamEncryption : IBundleEncryptor
{
    public BundleEncryptResult Encrypt(BundleEncryptArgs args)
    {
        var fileData = File.ReadAllBytes(args.FilePath);
        var fileDataSpan = fileData.AsSpan();
        for (int i = 0; i < fileData.Length; i++)
        {
            fileDataSpan[i] ^= BundleStream.KEY;
        }

        var result = new BundleEncryptResult(true, fileData);
        return result;
    }
}
