using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private async void Awake()
    {
        await YooAssetManager.Single.Initialize();
        var cube = YooAssetManager.Single.LoadAsset<GameObject>("Cube.prefab");
        GameObject.Instantiate(cube);
    }
}
