using UnityEngine;
using System.Collections;

public static class SingleRootManager
{
    private static GameObject root;
    public static GameObject Root
    {
        get
        {
            if (root == null)
            {
                root = new GameObject();
                root.name = "SingleRootManager";
                GameObject.DontDestroyOnLoad(root);
            }
            return root;
        }
    }
}
