using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
/// <summary>
/// 如果打包后的游戏目录路径有特殊字符可能会导致初始化失败,比如$
/// </summary>
public class YooAssetManager : SingleManager<YooAssetManager>
{
    private bool _isInitialize = false;
    public bool Initialized => _isInitialize;
    ResourcePackage resourcePackage;
    //Task设计允许外部等待， 以便在初始化成功前不做其他操作
    TaskCompletionSource<bool> taskCompletionSource;

    public async Task Initialize()
    {
        if (_isInitialize)
        {
            UnityEngine.Debug.LogWarning("YooAssets is initialized !");
            return;
        }
        taskCompletionSource = new TaskCompletionSource<bool>();
        YooAssets.Initialize();
        CreatePackage("MainPackage");
        await taskCompletionSource.Task;
    }

    public void CreatePackage(string packageName)
    {
        _isInitialize = true;

#if UNITY_EDITOR
        EditorSimulate(packageName);
#else
        OfflinePlayModeBuild(packageName);
#endif 

    }

    IEnumerator LoadPackage(InitializePackageOperation initOperation, ResourcePackage package)
    {
        if (initOperation.Status != EOperationStatus.Succeeded)
        {
            Debug.LogError($"资源包初始化失败：{initOperation.Error}");
            taskCompletionSource.SetResult(false);
            yield break;
        }
        var operation = package.RequestPackageVersionAsync();
        yield return operation;

        if (operation.Status == EOperationStatus.Succeeded)
        {
            //更新成功
            string packageVersion = operation.PackageVersion;
            Debug.Log($"Request package Version : {packageVersion}");
            var LoadPackageManifestAsyncOperation = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(packageVersion, 60));
            yield return LoadPackageManifestAsyncOperation;

            if (operation.Status == EOperationStatus.Succeeded)
            {
                resourcePackage = package;
                taskCompletionSource.SetResult(true);
                yield break;
            }
            else
            {
                //更新失败
                Debug.LogError(operation.Error);
            }
        }
        else
        {
            //更新失败
            Debug.LogError(operation.Error);
        }

        taskCompletionSource.SetResult(false);
    }

    private void EditorSimulate(string packageName)
    {
        var package = YooAssets.CreatePackage(packageName);
        var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
        var packageRoot = buildResult.PackageRootDirectory;

        var createParameters = new EditorSimulateModeOptions();
        createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

        var initOperation = package.InitializePackageAsync(createParameters);
        initOperation.Completed += (AsyncOperationBase) => StartCoroutine(LoadPackage(initOperation, package));

    }

    private void OfflinePlayModeBuild(string packageName)
    {
        var package = YooAssets.GetPackage(packageName);
        var fileSystemParams = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();

        var createParameters = new OfflinePlayModeOptions();
        createParameters.BuiltinFileSystemParameters = fileSystemParams;
        var initOperation = package.InitializePackageAsync(createParameters);
        initOperation.Completed += (AsyncOperationBase) => StartCoroutine(LoadPackage(initOperation, package));
    }

    public void Destroy()
    {
        if (!_isInitialize)
        {
            return;
        }
    }

    public ResourcePackage GetPackage(string packageName)
    {
        return YooAssets.GetPackage(packageName);
    }

    public bool ContainsPackage(string packageName)
    {
        return YooAssets.ContainsPackage(packageName);
    }

    /// <summary>
    /// 同步加载资源，立即释放yoo的引用计数
    /// </summary>
    /// <param name="location"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    public TObject LoadAsset<TObject>(string location) where TObject : UnityEngine.Object
    {
        var assetHandle = resourcePackage.LoadAssetSync<TObject>(location);
        var asset = assetHandle.AssetObject as TObject;
        assetHandle.Dispose();
        return asset;
    }

    /// <summary>
    /// 同步加载资源，返回引用句柄
    /// </summary>
    /// <param name="location"></param>
    /// <param name="assetHandle"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    public TObject LoadAsset<TObject>(string location, out AssetHandle assetHandle) where TObject : UnityEngine.Object
    {
        assetHandle = resourcePackage.LoadAssetSync<TObject>(location);
        return assetHandle.AssetObject as TObject;
    }

    /// <summary>
    /// 同步加载子资源对象
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    public SubAssetsHandle LoadSubAssets<TObject>(string location) where TObject : UnityEngine.Object
    {
        var subAssetsHandle = resourcePackage.LoadSubAssetsSync<TObject>(location);
        return subAssetsHandle;
    }

    /// <summary>
    /// 异步加载子资源对象
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    public SubAssetsHandle LoadSubAssetsAsync<TObject>(string location, uint priority = 0u) where TObject : UnityEngine.Object
    {
        return resourcePackage.LoadSubAssetsAsync<TObject>(location, priority);
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>
    public AssetHandle LoadAssetHandleAsync<TObject>(string location, uint priority = 0u) where TObject : UnityEngine.Object
    {
        return resourcePackage.LoadAssetAsync<TObject>(location, priority);
    }

    /// <summary>
    /// 异步加载资源包内所有TObject资源对象
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>    
    public AllAssetsHandle LoadAllAssetsAsync<TObject>(string location, uint priority = 0u) where TObject : UnityEngine.Object
    {
        return resourcePackage.LoadAllAssetsAsync<TObject>(location, priority);
    }

    /// <summary>
    /// 异步加载资源包内所有资源对象
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <typeparam name="TObject"></typeparam>
    /// <returns></returns>    
    public AllAssetsHandle LoadAllAssetsAsync(string location, uint priority = 0u)
    {
        return resourcePackage.LoadAllAssetsAsync(location, priority);
    }

    /// <summary>
    /// 加载原生文件
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public byte[] LoadRawFileBytes(string location)
    {
        var handle = resourcePackage.LoadAssetSync<RawFileObject>(location);
        handle.WaitForAsyncComplete();
        var rawFileObject = handle.GetAssetObject<RawFileObject>();
        byte[] fileData = rawFileObject.GetBytes();
        handle.Dispose();
        return fileData;
    }

    //
    /// <summary>
    /// 加载原生读取字符串
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public string LoadRawFileString(string location)
    {
        var handle = resourcePackage.LoadAssetSync<RawFileObject>(location);
        handle.WaitForAsyncComplete();
        var rawFileObject = handle.GetAssetObject<RawFileObject>();
        string fileText = rawFileObject.GetText();
        handle.Dispose();
        return fileText;
    }

    /// <summary>
    /// 异步加载原生文件
    /// </summary>
    /// <param name="location"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    public AssetHandle LoadRawFileAsync(string location, uint priority = 0u)
    {
        return resourcePackage.LoadAssetAsync<RawFileObject>(location, priority);
    }

    /// <summary>
    /// 同步加载场景
    /// </summary>
    /// <param name="location"></param>
    /// <param name="sceneMode"></param>
    /// <param name="localPhysicsMode"></param>
    /// <returns></returns>
    public SceneHandle LoadScene(string location, LoadSceneMode sceneMode = LoadSceneMode.Additive, LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None)
    {
        return resourcePackage.LoadSceneSync(location, sceneMode, localPhysicsMode);
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="location"></param>
    /// <param name="sceneMode"></param>
    /// <param name="localPhysicsMode"></param>
    /// <param name="allowSceneActivation"></param>
    /// <param name="priority"></param>
    /// <returns></returns>
    public SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Additive, LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None, bool allowSceneActivation = true, uint priority = 100u)
    {
        return resourcePackage.LoadSceneAsync(location, sceneMode, localPhysicsMode, allowSceneActivation, priority);
    }

    /// <summary>
    /// 同步加载资源包的地址，返回真实地址
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public string LoadEnsureBundleFilePath(string location)
    {
        var ensureBundleFileOperation = resourcePackage.EnsureBundleFileAsync(new EnsureBundleFileOptions(location));
        ensureBundleFileOperation.WaitForCompletion();
        string filePath = ensureBundleFileOperation.Detail.BundleFilePath;
        return filePath;
    }

    /// <summary>
    /// 异步加载资源包的地址
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public EnsureBundleFileOperation LoadEnsureBundleFileAsync(string location)
    {
        return resourcePackage.EnsureBundleFileAsync(new EnsureBundleFileOptions(location));
    }

    public void UnloadUnusedAssetsAsync()
    {
        resourcePackage.UnloadUnusedAssetsAsync();
    }
}