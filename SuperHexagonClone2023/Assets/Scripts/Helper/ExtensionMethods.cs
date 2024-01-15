using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomExtensions
{

    //It is common to create a class to contain all of your
    //extension methods. This class must be static.
    public static class ExtensionMethods
    {

        /* Transform Extensions */

        //Even though they are used like normal methods, extension
        //methods must be declared static. Notice that the first
        //parameter has the 'this' keyword followed by a Transform
        //variable. This variable denotes which class the extension
        //method becomes a part of.
        public static void ResetTransformation(this Transform trans)
        {
            trans.position = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = new Vector3(1, 1, 1);
        }


        /* Game Object Extensions */
        public static T GetRequiredComponent<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();

            if (component == null)
            {
                Debug.LogError("Expected to find component of type "
                   + typeof(T) + " but found none", obj);
            }

            return component;
        }

        public static GameObject FindRequired(this GameObject obj, string name)
        {
            GameObject go = GameObject.Find(name);

            if (go == null)
            {
                Debug.LogError("Expected to find game object with name of type "
                   + name + " but found none", obj);
            }

            return go;
        }

        public static GameObject FindRequiredGameObjectWithTag(this GameObject obj, string tag)
        {
            GameObject go = GameObject.FindGameObjectWithTag(tag);

            if (go == null)
            {
                Debug.LogError("Expected to find game object with name with tag "
                   + tag + " but found none", obj);
            }

            return go;
        }

        public static GameObject[] FindRequiredGameObjectsWithTag(this GameObject obj, string tag)
        {
            GameObject[] go = GameObject.FindGameObjectsWithTag(tag);

            if (go == null)
            {
                Debug.LogError("Expected to find at least one game object with name with tag "
                   + tag + " but found none", obj);
            }

            return go;
        }

        public static T GetComponentInParents<T>(this GameObject gameObject) where T : Component
        {
            for (Transform t = gameObject.transform; t != null; t = t.parent)
            {
                T result = t.GetComponent<T>();
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static T[] GetComponentsInParents<T>(this GameObject gameObject) where T : Component
        {
            List<T> results = new List<T>();
            for (Transform t = gameObject.transform; t != null; t = t.parent)
            {
                T result = t.GetComponent<T>();
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results.ToArray();
        }

        //https://answers.unity.com/questions/1034471/c-convert-vector3-to-vector2.html
        public static Vector2[] ToVector2Array(this Vector3[] v3)
        {
            return System.Array.ConvertAll<Vector3, Vector2>(v3, GetV3FromV2);
        }

        public static Vector2 GetV3FromV2(Vector3 v3)
        {
            return new Vector2(v3.x, v3.y);
        }

    }
}