using System;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;
    public SerializableVector3 playerPosition;

    public SaveData(string sceneName, Vector3 playerPos)
    {
        this.sceneName = sceneName;
        this.playerPosition = new SerializableVector3(playerPos);
    }
}

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}
