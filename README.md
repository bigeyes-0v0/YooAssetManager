# YooAssetManager

**YooAssetManager**

YooAssetManager是一套基于yooasset3开发的资源收集系统。 

它通过文件夹名字匹配自动实现yooasset所需要的资源收集 

只需要一个`$`符号，即可将当前目录下的资源收集进同一个bundle

**YooAsset**

YooAsset是一套用于Unity3D的资源管理系统，用于帮助研发团队快速部署和交付游戏。

官方github

https://github.com/tuyoogame/YooAsset

官方主页（教程文档）

https://www.yooasset.com/

**资源收集规则**

尽量不要在子文件夹嵌套带有$前置符号，可能会引起解析错误

所有资源都会打进MainPackage/MainGroup，需要多package多group的话自行修改

- **文件夹命名前置符号**

    | 命名符号 | 收集器类型 | 写入清单 | 功能描述 | 举例 |
    |---------|-----------|---------|---------|------|
    | `$` | MainAssetCollector | 是 | 收集当前目录下所有资源为一个bundle，可通过yooasset加载 | — |
    | `$1$` | StaticAssetCollector | 否 | 收集当前目录下所有资源为一个bundle，无法通过yooasset加载 | — |
    | `$2$` | DependAssetCollector | 否 | 只收集当前目录下被依赖的资源为一个bundle，无法通过yooasset加载 | — |
    | `$@文件后缀$` | MainAssetCollector | 是 | 与`$`功能一致，但只收集指定类型的文件 | `$@png$` 只收集png文件；`$@prefab$` 只收集预制体 |
    | `$1@文件后缀$` | StaticAssetCollector | 否 | 在`$1$`基础上限制收集的文件类型 | `$1@png$` 只收集png文件，不写入清单 |
    | `$@!文件后缀$` | MainAssetCollector | 是 | 与`$`功能一致，但排除指定类型 | `$@!png$` 排除png文件；`$@!prefab$` 排除预制体 |
    | `$1@!文件后缀$` | StaticAssetCollector | 否 | 在`$1$`基础上排除指定类型 | `$1@!png$` 排除png文件，不写入清单 |
    | `$@文件后缀@文件后缀$` | MainAssetCollector | 是 | 与`$@文件后缀$`功能一致，但限制多个文件类型（@可叠加多个） | `$@png@prefab$` 收集png文件和预制体 |
    | `$1@文件后缀@文件后缀$` | StaticAssetCollector | 否 | 与`$1@文件后缀$`功能一致，但限制多个文件类型 | `$1@png@prefab$` 收集png文件和预制体，不写入清单 |
    | `$@!文件后缀@!文件后缀$` | MainAssetCollector | 是 | 与`$@!文件后缀$`功能一致，但限制多个文件类型 | `$@png@!prefab$` 排除png文件和预制体 |
    | `$1@!文件后缀@!文件后缀$` | StaticAssetCollector | 否 | 与`$1@!文件后缀$`功能一致，但限制多个文件类型 | `$1@png@!prefab$` 排除png文件和预制体，不写入清单 |
    | `#` | — | — | 排除该文件夹 | — |

- **文件命名前置符号**

    **`#`**：排除该文件

**资源加载规则**

    文件名.后缀
    例如
        cube.prefab
        cube.mat 
        
# SpriteAtlasManager 

是一套基于yooasset3开发的资源sprite加载系统
根据sprite名称实现查找图集读取内部sprite
要求spriteName全局唯一
自动收集yooasset收集器类型为MainAssetCollector下的所有图集
支持根据spriteName释放图集的引用

# SingleManager

MonoBehaviour静态基类
 
# 安装说明

需要3个依赖才可以正常运作

名称            安装方式

2D Sprite       PackageManager

Collections     PackageManager

Yooasset3        参考yooasset安装方式