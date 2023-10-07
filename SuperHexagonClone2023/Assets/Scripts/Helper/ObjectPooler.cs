/*using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPoolItem
{
    public int amountToPool;
    public GameObject objectToPool;
    public bool shouldExpand;
    public int expandStep;
}

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler SharedInstance;
    public List<ObjectPoolItem> itemsToPool;
    public List<Queue<GameObject>> freeQueues;

    [Header("Debug")]
    public bool verboseDebug = false;
    void Awake()
    {
        SharedInstance = this;
        freeQueues = new List<Queue<GameObject>>();
        int i = 0;
        foreach (ObjectPoolItem item in itemsToPool)
        {
            freeQueues.Add(new Queue<GameObject>());
            for (int j = 0; j < item.amountToPool; j++)
            {
                GameObject obj = (GameObject)Instantiate(item.objectToPool);
                obj.SetActive(false);
                freeQueues[i].Enqueue(obj);

                obj.transform.parent = gameObject.transform;
            }
            i++;
        }
    }

    public GameObject GetPooledObject(string searchTag)
    {
        int itemOffset = -1;
        int i = 0;
        while (i < itemsToPool.Count)
        {
            if (itemsToPool[i].objectToPool.tag == searchTag)
            {
                itemOffset = i;
                break;
            }
            i++;
        }
        if (itemOffset == -1)
        {
            //ERROR item not stored in this pool
        }

        if (freeQueues[i] != null)
        {//&& freeQueues[i].Count > 0) {

            if (freeQueues[i].Count == 0)
            {
                if (itemsToPool[i].shouldExpand)
                {
                    Debug.Log("Expanding the pool");
                    int step = itemsToPool[i].expandStep;
                    for (int j = 0; j < step; j++)
                    {
                        GameObject newItem = Instantiate(itemsToPool[i].objectToPool);
                        newItem.SetActive(false);
                        freeQueues[i].Enqueue(newItem);
                    }
                }
                else
                {
                    //ERROR there are no more items left in the list and this item isn't allowed to have more created    : return null;
                }
            }
            //If the code reaches this point, then there must be an item available in the pool free list
            if (freeQueues[i].Peek().tag == searchTag)
            {
                itemOffset = i;

                GameObject item = freeQueues[i].Dequeue();
                if (verboseDebug)
                {
                    Debug.Log("The object is in the pool and there are " + freeQueues[i].Count + " remaining");
                }
                item.SetActive(true);
                return item;
            }
            else
            {
                if (freeQueues[i].Count == 0)
                {
                }
            }
        }
        else
        {
            throw new UnityException("Object pool doesn't contain the requested object.");
        }

        return null;
    }

    public void RecycleObject(GameObject toRecycle)
    {
        toRecycle.SetActive(false);
        int i = 0;
        while (i < itemsToPool.Count)
        {
            if (toRecycle.tag == itemsToPool[i].objectToPool.tag)
            {
                break;
            }
        }

        if (freeQueues[i] != null)
        {
        }

        freeQueues[i].Enqueue(toRecycle);

        if (verboseDebug)
        {
            Debug.Log("Recycled " + toRecycle.name + " and there are " + freeQueues[i].Count + " items of this type free. ");
        }

        toRecycle.transform.parent = gameObject.transform;

    }
}

*/