using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SingleManager<T> : MonoBehaviour where T : SingleManager<T>
{
    private static T single;
    private static bool isCreate;

    public static T Single
    {
        get
        {
            if (!isCreate)
            {
                Init();
            }
            return single;
        }
    }

    static void Init()
    {
        if (Application.isPlaying)
        {
            var typeName = typeof(T).Name;
            var manager = SingleRootManager.Root.transform.Find(typeName);
            if (manager != null)
            {
                single = manager.GetComponent<T>();
            }
            else
            {
                single = new GameObject(typeName).gameObject.AddComponent<T>();
                single.transform.SetParent(SingleRootManager.Root.transform, false);
            }
        }
        else
        {
            throw new Exception("SingleManager 只能在运行时获取");
        }
        isCreate = true;
    }

    protected void SetManager(T manager)
    {
        single = manager;
        isCreate = true;
        manager.transform.SetParent(SingleRootManager.Root.transform, false);
    }

    public void Touch()
    {

    }
}
