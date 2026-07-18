# YooAssetManager

**YooAssetManager** 是一套基于 [YooAsset 3](https://www.yooasset.com/) 开发的资源管理框架，包含资源自动收集、资源加载、精灵图集管理和单例管理器四大模块。

只需一个 `$` 符号，即可将当前目录下的资源自动收集进同一个 AssetBundle，无需手动配置 YooAsset Collector。

---

## 目录

- [功能特性](#功能特性)
- [安装说明](#安装说明)
- [目录结构](#目录结构)
- [资源收集规则](#资源收集规则)
- [YooAssetManager 使用方式](#yooassetmanager-使用方式)
- [SpriteAtlasManager 使用方式](#spriteatlasmanager-使用方式)
- [SingleManager 使用方式](#singlemanager-使用方式)
- [资源加密与解密](#资源加密与解密)
- [编辑器菜单](#编辑器菜单)
- [许可证](#许可证)

---

## 功能特性

- **自动收集**：通过文件夹命名规则自动配置 YooAsset Collector，无需手动维护收集器列表
- **自动刷新**：资源导入/移动/删除时自动刷新收集配置（`AssetPostprocessor`）
- **文件类型过滤**：支持按扩展名白名单/黑名单过滤收集的资源
- **资源加密**：内置 XOR 加密/解密流，编辑器打包时加密，运行时自动解密
- **精灵图集管理**：根据 sprite 名称直接加载图集中的精灵，支持 SpriteAtlas / Multiple Texture2D / Single Texture2D 三种形式
- **单例基类**：提供 `SingleManager<T>` 泛型单例基类，自动创建并挂载到 DontDestroyOnLoad 根节点

---

## 安装说明

需要以下 3 个依赖：

| 名称 | 安装方式 |
|------|---------|
| **2D Sprite** | Unity Package Manager（`com.unity.2d.sprite`） |
| **Collections** | Unity Package Manager（`com.unity.collections`） |
| **YooAsset 3** | 参考 [YooAsset 官方安装方式](https://www.yooasset.com/docs/guide-editor/QuickStart) |

将 `YooAssetManager` 文件夹放入 Unity 项目的 `Assets/` 目录下即可。

---

## 目录结构

```
YooAssetManager/
├── Editor/
│   ├── AutoRefreshYooAssetBundleEditor.cs   # 资源导入回调，自动刷新收集配置
│   ├── FileStreamEncryption.cs               # 打包时资源加密（IBundleEncryptor）
│   ├── YooAssetSettingsUtility.cs            # 核心：扫描 $ 文件夹并生成 Collector
│   ├── IAddressRule/
│   │   └── AddressByFileNameType.cs          # 寻址规则：文件名.后缀
│   └── IFilterRule/
│       ├── CollectAllIgnoring.cs             # 收集所有资源（支持 @ext 过滤）
│       ├── CollectPrefabgnoring.cs           # 只收集预制体
│       ├── CollectSceneIgnoring.cs           # 只收集场景
│       ├── CollectShaderIgnoring.cs           # 只收集 Shader
│       ├── CollectShaderVariantsIgnoring.cs  # 只收集 Shader 变体
│       └── CollectSpriteIgnoring.cs          # 只收集精灵图
├── Runtime/
│   ├── YooAssetManager.cs                    # 运行时资源加载管理器
│   ├── BundleStream.cs                      # XOR 解密流
│   └── FileStreamDecryption.cs              # 运行时资源解密（IBundleStreamDecryptor）
├── SingleManager/
│   ├── SingleManager.cs                     # 泛型单例基类
│   └── SingleRootManager.cs                 # 单例根节点（DontDestroyOnLoad）
├── SpriteAtlasManager/
│   ├── Editor/
│   │   └── SpriteAtlasUtility.cs             # 图集精灵收集工具
│   └── Runtime/
│       ├── SpriteAtlasManager.cs             # 精灵图集加载管理器
│       └── SpriteAtlasData.cs               # 图集数据 ScriptableObject
├── Resources/
│   └── YooAssetSettings.asset                # YooAsset 配置文件
├── README.md
└── LICENSE.md
```

---

## 资源收集规则

### 文件夹命名规则

在 `Assets/` 下创建以 `$` 开头的文件夹，系统会自动将其注册为 YooAsset Collector。所有资源默认收集进 `MainPackage / MainGroup`。

> ⚠️ 尽量不要在子文件夹中嵌套带有 `$` 前缀的文件夹，可能会引起解析错误。

| 命名格式 | 收集器类型 | 写入清单 | 功能描述 | 举例 |
|---------|-----------|---------|---------|------|
| `$...` | MainAssetCollector | 是 | 收集当前目录下所有资源为一个 bundle，可通过 YooAsset 加载 | `$UI` |
| `$1$...` | StaticAssetCollector | 否 | 收集当前目录下所有资源为一个 bundle，无法通过 YooAsset 加载 | `$1$Configs` |
| `$2$...` | DependAssetCollector | 否 | 只收集被依赖的资源为一个 bundle，无法通过 YooAsset 加载 | `$2$Shared` |
| `$@ext$...` | MainAssetCollector | 是 | 与 `$` 一致，但只收集指定类型的文件 | `$@png$Icons` 只收集 png |
| `$1@ext$...` | StaticAssetCollector | 否 | 在 `$1$` 基础上限制文件类型 | `$1@png$Tex` |
| `$@!ext$...` | MainAssetCollector | 是 | 与 `$` 一致，但排除指定类型 | `$@!png$Models` 排除 png |
| `$1@!ext$...` | StaticAssetCollector | 否 | 在 `$1$` 基础上排除文件类型 | `$1@!png$Tex` |
| `$@ext@ext$...` | MainAssetCollector | 是 | 限制多个文件类型（`@` 可叠加） | `$@png@prefab$Assets` |
| `$@!ext@!ext$...` | MainAssetCollector | 是 | 排除多个文件类型 | `$@!png@!prefab$Assets` |
| `#...` | — | — | 排除该文件夹 | `#Temp` |

> `...` 表示文件夹的自定义名称部分，可以省略（如 `$` 本身就是一个合法的收集文件夹名）。

### 文件命名规则

- **`#`**：以 `#` 开头的文件会被排除，不参与收集。

### 资源寻址规则

加载资源时使用 **文件名.后缀** 作为地址：

```
Cube.prefab
Hero.mat
bgm.wav
```

---

## YooAssetManager 使用方式

### 1. 初始化

在游戏启动时调用 `Initialize()`，该方法返回 `Task`，可在异步方法中 `await`：

```csharp
using UnityEngine;
using System.Threading.Tasks;

public class GameStart : MonoBehaviour
{
    private async void Awake()
    {
        await YooAssetManager.Single.Initialize();
        // 初始化完成后即可加载资源
        var cube = YooAssetManager.Single.LoadAsset<GameObject>("Cube.prefab");
        Instantiate(cube);
    }
}
```

> - 编辑器下使用 `EditorSimulate` 模式，打包后使用 `OfflinePlay` 模式，自动切换，无需关心。
> - `YooAssetManager.Single` 是通过 `SingleManager<T>` 实现的单例，首次访问时自动创建 GameObject。

### 2. 同步加载资源

```csharp
// 方式一：加载后立即释放引用计数（适合不需要后续操作的资源）
var material = YooAssetManager.Single.LoadAsset<Material>("Hero.mat");

// 方式二：加载并保留引用句柄（需要手动 Dispose 释放）
var prefab = YooAssetManager.Single.LoadAsset<GameObject>("Cube.prefab", out var handle);
var go = Instantiate(prefab);
// ... 使用完毕后释放
handle.Dispose();
```

### 3. 异步加载资源

```csharp
// 返回 AssetHandle，通过 await 或协程等待完成
var handle = YooAssetManager.Single.LoadAssetHandleAsync<GameObject>("Cube.prefab");
await handle.Task;
var prefab = handle.AssetObject as GameObject;
Instantiate(prefab);
// 使用完毕后释放
handle.Dispose();
```

### 4. 加载子资源

适用于 Sprite Sheet、Mesh 等包含多个子资源的文件：

```csharp
// 同步
var subHandle = YooAssetManager.Single.LoadSubAssets<Sprite>("Icons.png");
var sprite = subHandle.GetSubAssetObject<Sprite>("icon_01");

// 异步
var subHandleAsync = YooAssetManager.Single.LoadSubAssetsAsync<Sprite>("Icons.png");
await subHandleAsync.Task;
```

### 5. 加载 Bundle 内所有资源

```csharp
// 加载指定类型的所有资源
var allHandle = YooAssetManager.Single.LoadAllAssetsAsync<Sprite>("Icons.png");
await allHandle.Task;
foreach (var sprite in allHandle.AllAssetObjects)
{
    Debug.Log(sprite.name);
}

// 加载所有资源（不限定类型）
var allHandle2 = YooAssetManager.Single.LoadAllAssetsAsync("Icons.png");
```

### 6. 加载原生文件

适用于文本、二进制等非 Unity 资源：

```csharp
// 加载为字节数组
byte[] bytes = YooAssetManager.Single.LoadRawFileBytes("config.json");

// 加载为字符串
string json = YooAssetManager.Single.LoadRawFileString("config.json");

// 异步加载
var rawHandle = YooAssetManager.Single.LoadRawFileAsync("config.json");
await rawHandle.Task;
var rawObj = rawHandle.GetAssetObject<RawFileObject>();
string text = rawObj.GetText();
rawHandle.Dispose();
```

### 7. 加载场景

```csharp
// 同步加载（默认 Additive 模式）
var sceneHandle = YooAssetManager.Single.LoadScene("Battle.unity");

// 异步加载
var sceneHandle = YooAssetManager.Single.LoadSceneAsync("Battle.unity", LoadSceneMode.Single);
await sceneHandle.Task;
// 卸载场景
sceneHandle.UnloadAsync();
```

### 8. 获取 Bundle 文件路径

```csharp
// 同步获取真实文件路径
string filePath = YooAssetManager.Single.LoadEnsureBundleFilePath("Cube.prefab");

// 异步获取
var op = YooAssetManager.Single.LoadEnsureBundleFileAsync("Cube.prefab");
await op.Task;
string path = op.Detail.BundleFilePath;
```

### 9. 卸载未使用资源

```csharp
YooAssetManager.Single.UnloadUnusedAssetsAsync();
```

### 10. 多 Package 管理

```csharp
// 获取指定 Package
var package = YooAssetManager.Single.GetPackage("MainPackage");

// 检查 Package 是否存在
bool exists = YooAssetManager.Single.ContainsPackage("MainPackage");
```

### API 速查表

| 方法 | 说明 |
|------|------|
| `Initialize()` | 初始化资源系统，返回 Task |
| `LoadAsset<T>(location)` | 同步加载，立即释放引用 |
| `LoadAsset<T>(location, out handle)` | 同步加载，保留引用句柄 |
| `LoadAssetHandleAsync<T>(location)` | 异步加载，返回 AssetHandle |
| `LoadSubAssets<T>(location)` | 同步加载子资源 |
| `LoadSubAssetsAsync<T>(location)` | 异步加载子资源 |
| `LoadAllAssetsAsync<T>(location)` | 异步加载所有 T 类型资源 |
| `LoadAllAssetsAsync(location)` | 异步加载所有资源 |
| `LoadRawFileBytes(location)` | 同步加载原生文件为 byte[] |
| `LoadRawFileString(location)` | 同步加载原生文件为 string |
| `LoadRawFileAsync(location)` | 异步加载原生文件 |
| `LoadScene(location)` | 同步加载场景 |
| `LoadSceneAsync(location)` | 异步加载场景 |
| `LoadEnsureBundleFilePath(location)` | 同步获取 Bundle 文件路径 |
| `LoadEnsureBundleFileAsync(location)` | 异步获取 Bundle 文件路径 |
| `UnloadUnusedAssetsAsync()` | 卸载未使用资源 |
| `GetPackage(name)` | 获取指定 Package |
| `ContainsPackage(name)` | 检查 Package 是否存在 |

---

## SpriteAtlasManager 使用方式

SpriteAtlasManager 是一套基于 YooAsset 3 的精灵图加载系统，根据 sprite 名称直接查找并加载图集中的精灵。

### 特性

- 根据 sprite 名称全局查找，无需关心 sprite 在哪个图集中
- 自动收集所有 MainAssetCollector 下的 SpriteAtlas、Multiple Texture2D、Single Texture2D
- 要求 sprite 名称全局唯一（重复收集会在控制台输出警告）
- 支持按 sprite 名称释放图集引用

### 前置条件

1. 确保已通过 YooAssetManager 初始化资源系统
2. 确保 SpriteAtlas 资源所在文件夹使用了 `$` 前缀（MainAssetCollector 类型）
3. 在编辑器中执行 `YooAsset > RefreshSpriteAtlas` 生成 `SpriteAtlasData.asset`

### 使用示例

```csharp
using UnityEngine;
using UnityEngine.UI;

public class TestSpriteAtlas : MonoBehaviour
{
    public Image image;
    public string spriteName = "icon_01";

    async void Start()
    {
        // 1. 先初始化 YooAssetManager
        await YooAssetManager.Single.Initialize();

        // 2. 通过 sprite 名称获取精灵
        image.sprite = SpriteAtlasManager.Single.GetSprite(spriteName);
    }

    void OnDestroy()
    {
        // 3. 释放精灵所在图集的引用
        // 注意：释放前需要先将 Image.sprite 设为 null
        image.sprite = null;
        SpriteAtlasManager.Single.ReleaseSprite(spriteName);

        // 4. 卸载未使用资源
        YooAssetManager.Single.UnloadUnusedAssetsAsync();
    }
}
```

### API

| 方法 | 说明 |
|------|------|
| `GetSprite(spriteName)` | 同步获取精灵（不包含后缀名） |
| `ReleaseSprite(spriteName)` | 释放精灵所在图集的引用 |

> ⚠️ `ReleaseSprite` 会释放该图集中所有精灵的引用，外部所有使用了该 sprite 的 Unity 组件都需要先手动设为 `null`（如 `image.sprite = null`）才能实现资源卸载。

---

## SingleManager 使用方式

`SingleManager<T>` 是一个泛型 MonoBehaviour 单例基类，自动创建并挂载到 `SingleRootManager` 根节点下（`DontDestroyOnLoad`）。

### 基本用法

```csharp
using UnityEngine;

// 继承 SingleManager<T> 实现单例
public class GameManager : SingleManager<GameManager>
{
    public int Score { get; set; }

    void Awake()
    {
        // 初始化逻辑
    }
}

// 在任意位置通过 .Single 访问
GameManager.Single.Score += 10;
```

### 注意事项

- `SingleManager<T>` 只能在运行时（`Application.isPlaying`）获取，编辑器模式下会抛出异常
- 首次访问 `.Single` 时自动创建 GameObject 并挂载组件
- 所有单例 GameObject 都挂载在 `SingleRootManager` 根节点下，场景切换时不会被销毁

---

## 资源加密与解密

框架内置了基于 XOR 的资源加密/解密机制。

### 加密（编辑器打包时）

`FileStreamEncryption` 实现了 YooAsset 的 `IBundleEncryptor` 接口，在打包时对 Bundle 文件进行 XOR 加密。

```csharp
// 加密密钥
public const byte KEY = 11 | 45 ^ 14;
```

### 解密（运行时加载时）

`FileStreamDecryption` 实现了 YooAsset 的 `IBundleStreamDecryptor` 接口，在运行时通过 `BundleStream` 对加密的 Bundle 文件进行 XOR 解密。

### 配置

在 YooAsset 的打包配置中：
- **加密类** 选择 `FileStreamEncryption`
- **解密类** 选择 `FileStreamDecryption`

---

## 编辑器菜单

| 菜单路径 | 功能 |
|---------|------|
| `YooAsset/Refresh` | 手动刷新资源收集配置 + 图集数据 |
| `YooAsset/RefreshSpriteAtlas` | 手动刷新精灵图集数据 |

> 正常情况下无需手动执行，资源导入/移动时会自动触发刷新。

---

## 许可证

本项目基于 [Apache License 2.0](LICENSE.md) 开源。
