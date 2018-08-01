using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComponentExtension {
    public static string GetPath(this Component current) {
        return current.transform.GetPath() + "/" + current.GetType().Name;
    }
}
