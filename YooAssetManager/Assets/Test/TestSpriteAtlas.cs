using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestSpriteAtlas : MonoBehaviour
{
    public List<TestSprite> testSprites = new();
    public float unloadUnusedTime = 5;
    async void Start()
    {
        await YooAssetManager.Single.Initialize();
        Refresh();
    }

    void Refresh()
    {
        foreach (var item in testSprites)
        {
            item.image.sprite = SpriteAtlasManager.Single.GetSprite(item.spriteName);
        }
    }

    private float time = 0;
    bool isUnloaded = false;
    private void Update()
    {
        time += Time.deltaTime;
        if (isUnloaded == false && time > unloadUnusedTime)
        {
            isUnloaded = true;
            foreach (var item in testSprites)
            {
                SpriteAtlasManager.Single.ReleaseSprite(item.spriteName);
            }
            YooAssetManager.Single.UnloadUnusedAssetsAsync();
        }
    }
    [Serializable]
    public struct TestSprite
    {
        public Image image;
        public string spriteName;
    }
}
