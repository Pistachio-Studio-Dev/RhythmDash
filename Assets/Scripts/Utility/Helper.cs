using UnityEngine;

public static class Helper
{
    public static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform current = go.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    public static GameObject FindRootObjectWithTag(GameObject gameObject, string tag)
    {
        GameObject parent = gameObject.transform.parent != null ? gameObject.transform.parent.gameObject : null;
        
        while(parent != null && parent.CompareTag(tag))
        {
            gameObject = parent;
            parent = parent.transform.parent != null ? parent.transform.parent.gameObject : null;
        }
        
        return gameObject;
    }
}
