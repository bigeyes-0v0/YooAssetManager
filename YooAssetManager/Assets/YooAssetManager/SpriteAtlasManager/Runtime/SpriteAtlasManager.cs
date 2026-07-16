using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;
public class SpriteAtlasManager : SingleManager<SpriteAtlasManager>
{
    public const string SpriteAtlasDataName = "SpriteAtlasData.asset";
    Dictionary<int, SpriteAtlasHandleData> spriteAtlasHandleDataDict = new();
    Dictionary<int, SubAssetsHandle> multipleTexture2DDict = new();
    Dictionary<int, AssetHandle> singleSpriteDict = new();
    Dictionary<string, SpriteData> spriteDataDict;
    SpriteAtlasData spriteAtlasData;
    AssetHandle spriteAtlasDataHandle;

    private void Awake()
    {
        spriteAtlasData = YooAssetManager.Single.LoadAsset<SpriteAtlasData>(SpriteAtlasDataName, out spriteAtlasDataHandle);
        ReadOnlySpan<SpriteAtlasData.SaveData> spriteAtlasSaveDataSpan = spriteAtlasData.spriteAtlasSaveDatas.AsSpan();
        ReadOnlySpan<SpriteAtlasData.SaveData> multipleTexture2DSaveDataSpan = spriteAtlasData.multipleTexture2DSaveDatas.AsSpan();
        ReadOnlySpan<string> singleSpritefileNameSpan = spriteAtlasData.singleSpritefileNames.AsSpan();

        var spriteCount = singleSpritefileNameSpan.Length;
        foreach (var item in spriteAtlasSaveDataSpan)
        {
            spriteCount += item.spriteNames.Length;
        }
        foreach (var item in multipleTexture2DSaveDataSpan)
        {
            spriteCount += item.spriteNames.Length;
        }
        spriteDataDict = new(spriteCount);

        var length = spriteAtlasSaveDataSpan.Length;
        for (var i = 0; i < length; i++)
        {
            ref readonly var saveData = ref spriteAtlasSaveDataSpan[i];
            ReadOnlySpan<string> spriteNameSpan = saveData.spriteNames.AsSpan();
            foreach (var spriteName in spriteNameSpan)
            {
                spriteDataDict.Add(spriteName, new(i, SpriteType.SpriteAtlas));
            }
        }

        length = multipleTexture2DSaveDataSpan.Length;
        for (var i = 0; i < length; i++)
        {
            ref readonly var saveData = ref multipleTexture2DSaveDataSpan[i];
            ReadOnlySpan<string> spriteNameSpan = saveData.spriteNames.AsSpan();
            foreach (var spriteName in spriteNameSpan)
            {
                spriteDataDict.Add(spriteName, new(i, SpriteType.Multiple));
            }
        }
        length = singleSpritefileNameSpan.Length;
        for (var i = 0; i < length; i++)
        {
            spriteDataDict.Add(Path.GetFileNameWithoutExtension(singleSpritefileNameSpan[i]), new(i, SpriteType.Single));
        }
    }


    /// <summary>
    /// 同步加载精灵图
    /// 卸载图集的时候需要自己手动image.sprite=null
    /// </summary>
    /// <param name="spriteName">不包含后缀</param>
    /// <returns></returns>
    public Sprite GetSprite(string spriteName)
    {
        if (spriteDataDict.TryGetValue(spriteName, out var spriteData))
        {
            if (spriteData.isCreate)
                return spriteData.sprite;
            switch (spriteData.spriteType)
            {
                case SpriteType.SpriteAtlas: LoadSpriteOfSpriteAtlas(spriteName, ref spriteData); break;
                case SpriteType.Multiple: LoadSpriteOfMultiple(spriteName, ref spriteData); break;
                case SpriteType.Single: LoadSpriteOfSingle(ref spriteData); break;
                default:
                    throw new Exception("GetSprite 错误的SpriteType类型");
            }
            spriteDataDict[spriteName] = spriteData;
            return spriteData.sprite;
        }
        else
        {
            Debug.LogWarning($"name: [{spriteName}] GetSprite 可能不是一个sprite,或者未收集");
            return null;
        }
    }

    /// <summary>
    /// 释放精灵图所在的图集的引用
    /// 这会释放这个图集里所有的精灵图的引用
    /// 外部所有使用了这个sprite的unity组件 都需要手动image.sprite=null 才能实现资源卸载
    /// </summary>
    /// <param name="spriteName">不包含后缀</param>
    public void ReleaseSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return;
        }

        if (spriteDataDict.TryGetValue(spriteName, out var spriteData))
        {
            if (spriteData.isCreate == false)
                return;
            switch (spriteData.spriteType)
            {
                case SpriteType.SpriteAtlas: ReleaseSpriteOfSpriteAtlas(spriteName, spriteData); break;
                case SpriteType.Multiple: ReleaseSpriteOfMultiple(spriteName, spriteData); break;
                case SpriteType.Single: ReleaseSpriteOfSingle(spriteName, spriteData); break;
                default:
                    throw new Exception("GetSprite 错误的SpriteType类型");
            }
        }
        else
        {
            Debug.LogWarning($"{spriteName} ReleaseSprite 可能不是一个sprite,或者未收集");
            return;
        }
    }

    void ReleaseSpriteOfSpriteAtlas(string spriteName, in SpriteData spriteData)
    {
        if (spriteAtlasHandleDataDict.TryGetValue(spriteData.index, out var handleData))
        {
            ReadOnlySpan<string> spriteNames = spriteAtlasData.spriteAtlasSaveDatas[spriteData.index].spriteNames.AsSpan();
            foreach (var item in spriteNames)
            {
                if (spriteDataDict.TryGetValue(item, out var data))
                {
                    if (data.isCreate)
                    {
                        Destroy(data.sprite);
                        data.sprite = null;
                        data.isCreate = false;
                        spriteDataDict[spriteName] = data;
                    }
                }
            }
            handleData.spriteAtlas = null;
            handleData.assetHandle.Dispose();
            spriteAtlasHandleDataDict.Remove(spriteData.index);
        }
    }

    void ReleaseSpriteOfMultiple(string spriteName, in SpriteData spriteData)
    {
        if (multipleTexture2DDict.TryGetValue(spriteData.index, out var handleData))
        {
            ReadOnlySpan<string> spriteNames = spriteAtlasData.multipleTexture2DSaveDatas[spriteData.index].spriteNames.AsSpan();
            foreach (var item in spriteNames)
            {
                if (spriteDataDict.TryGetValue(item, out var data))
                {
                    if (data.isCreate)
                    {
                        data.sprite = null;
                        data.isCreate = false;
                        spriteDataDict[spriteName] = data;
                    }
                }
            }
            handleData.Dispose();
            multipleTexture2DDict.Remove(spriteData.index);
        }
    }

    void ReleaseSpriteOfSingle(string spriteName, SpriteData spriteData)
    {
        if (singleSpriteDict.TryGetValue(spriteData.index, out var handleData))
        {
            if (spriteData.isCreate)
            {
                spriteData.sprite = null;
                spriteData.isCreate = false;
                spriteDataDict[spriteName] = spriteData;
            }
            handleData.Dispose();
            singleSpriteDict.Remove(spriteData.index);
        }
    }

    void LoadSpriteOfSpriteAtlas(string spriteName, ref SpriteData spriteData)
    {
        if (!spriteAtlasHandleDataDict.TryGetValue(spriteData.index, out var handleData))
        {
            handleData.spriteAtlas = YooAssetManager.Single.LoadAsset<SpriteAtlas>(spriteAtlasData.spriteAtlasSaveDatas[spriteData.index].fileName, out var assetHandle);
            handleData.assetHandle = assetHandle;
            spriteAtlasHandleDataDict.Add(spriteData.index, handleData);
        }
        spriteData.SetSprite(handleData.spriteAtlas.GetSprite(spriteName));
    }

    void LoadSpriteOfMultiple(string spriteName, ref SpriteData spriteData)
    {
        if (!multipleTexture2DDict.TryGetValue(spriteData.index, out var subAssetsHandle))
        {
            subAssetsHandle = YooAssetManager.Single.LoadSubAssets<Sprite>(spriteAtlasData.multipleTexture2DSaveDatas[spriteData.index].fileName);
            multipleTexture2DDict.Add(spriteData.index, subAssetsHandle);
        }
        spriteData.SetSprite(subAssetsHandle.GetSubAssetObject<Sprite>(spriteName));
    }

    void LoadSpriteOfSingle(ref SpriteData spriteData)
    {
        Sprite sprite = null;
        if (!singleSpriteDict.TryGetValue(spriteData.index, out var assetHandle))
        {
            sprite = YooAssetManager.Single.LoadAsset<Sprite>(spriteAtlasData.singleSpritefileNames[spriteData.index], out assetHandle);
            singleSpriteDict.Add(spriteData.index, assetHandle);
        }
        spriteData.SetSprite(sprite);
    }

    struct SpriteData
    {
        public Sprite sprite;
        //图集名称的稀疏存储所在的下标
        public int index;
        public SpriteType spriteType;
        public bool isCreate;

        public SpriteData(int index, SpriteType spriteType)
        {
            this.index = index;
            this.spriteType = spriteType;
            sprite = null;
            isCreate = false;
        }

        public void SetSprite(Sprite sprite)
        {
            this.sprite = sprite;
            isCreate = sprite != null;
        }
    }

    struct SpriteAtlasHandleData
    {
        public SpriteAtlas spriteAtlas;
        public AssetHandle assetHandle;
    }

    enum SpriteType : byte
    {
        SpriteAtlas,
        Multiple,
        Single
    }

    //外部可能提前删除image，导致自动引用泄露
    // struct AutoReleaseHandleKey : IEquatable<AutoReleaseHandleKey>
    // {
    //     public int index;
    //     public SpriteType spriteType;

    //     public AutoReleaseHandleKey(int index, SpriteType spriteType)
    //     {
    //         this.index = index;
    //         this.spriteType = spriteType;
    //     }


    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public readonly bool Equals(AutoReleaseHandleKey other)
    //     {
    //         return GetHashCode() == other.GetHashCode();
    //     }

    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public readonly override bool Equals(object obj)
    //     {
    //         if (obj is AutoReleaseHandleKey a)
    //         {
    //             return this.Equals(a);
    //         }
    //         return false;
    //     }

    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     public readonly override int GetHashCode()
    //     {
    //         return (int)math.hash(new int2(index, (int)spriteType));
    //     }
    // }
}
