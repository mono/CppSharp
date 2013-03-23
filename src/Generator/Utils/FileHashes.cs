using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
class FileHashes 
{
    private string serializedFile;
    private Dictionary<string, int> fileHashes = new Dictionary<string, int>();

    public bool UpdateHash(string file, int hash)
    {
        if(!fileHashes.ContainsKey(file))
        {
            fileHashes.Add(file, hash);
            Save(this, serializedFile);
            return true;
        }

        var oldHash = fileHashes[file];
        fileHashes[file] = hash;
        Save(this, serializedFile);

        return oldHash != hash;
    }

    public static FileHashes Load(string file)
    {
        var stream = File.Open(file, FileMode.OpenOrCreate);
        var bformatter = new BinaryFormatter();

        FileHashes obj;
        if(stream.Length>0)
            obj = (FileHashes)bformatter.Deserialize(stream);
        else
            obj = new FileHashes();
        obj.serializedFile = file;
        stream.Close();

        return obj;
    }

    public static void Save(FileHashes obj, string file)
    {
        Stream stream = File.Open(file, FileMode.Create);
        BinaryFormatter bformatter = new BinaryFormatter();
            
        bformatter.Serialize(stream, obj);
        stream.Close();
    }
}