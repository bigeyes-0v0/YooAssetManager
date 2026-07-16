using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAtlasData : ScriptableObject
{
    [Serializable]
    public struct SaveData
    {
        /// <summary>
        /// 文件名.后缀
        /// </summary>
        public string fileName;
        /// <summary>
        /// 精灵图名称
        /// </summary>
        public string[] spriteNames;

        public SaveData(string fileName, string[] spriteNames)
        {
            this.fileName = fileName;
            this.spriteNames = spriteNames;
        }
    }

    public SaveData[] spriteAtlasSaveDatas;
    public SaveData[] multipleTexture2DSaveDatas;
    public string[] singleSpritefileNames;
}

