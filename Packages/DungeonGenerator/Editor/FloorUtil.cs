using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Xeon.Dungeon;

public static class FloorUtil
{
    public static string SavePath => Application.dataPath.Replace("Assets", "Generated");

    private static string GetFloorFilePath(string dungeonName, int floorNum) => System.IO.Path.Combine(SavePath, dungeonName, $"{floorNum}F.flr");

    public static void CleanupDungeonFolder(string dungeonName)
    {
        var directoryInfo = new DirectoryInfo(System.IO.Path.Combine(SavePath, dungeonName));
        if (!directoryInfo.Exists) return;
        foreach (var file in directoryInfo.GetFiles())
            file.Delete();
    }

    public static void CreateFolder(string dungeonName)
    {
        var directoryInfo = new DirectoryInfo(System.IO.Path.Combine(SavePath, dungeonName));
        if (directoryInfo.Exists) return;
        directoryInfo.Create();
    }

    public static bool Serialize(string dungeonName, FloorData data, int floorNum)
    {
        var filePath = GetFloorFilePath(dungeonName, floorNum);
        try
        {
            var formatter = new BinaryFormatter();
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
            using (var bw = new BinaryWriter(fs))
            {
                var bf = new BinaryFormatter();
                bf.Serialize(fs, data);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public static FloorData Deserialize(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            var bf = new BinaryFormatter();
            return bf.Deserialize(fs) as FloorData;
        }
    }

    public static FloorData Deserialize(string dungeonName, int floorNum)
    {
        var filePath = GetFloorFilePath(dungeonName, floorNum);
        return Deserialize(filePath);
    }
}
