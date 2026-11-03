using System.Collections.Generic;
using UnityEngine;

public static class ArtDb
{
    static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

    public static Texture2D Character(string characterId, string pose)
    {
        if (characterId == "minion") return Load("Art/minion");
        if (pose == "bow") return Load("Art/dragon-bow");
        if (pose == "talk" || pose == "nod" || pose == "unsure" || pose == "thumb" || pose == "smile")
            return Load("Art/dragon-talk");
        return Load("Art/dragon");
    }

    public static Texture2D Background(string backgroundId)
    {
        if (backgroundId == "park") return Load("Art/bg-park");
        if (backgroundId == "night") return Load("Art/bg-night");
        return Load("Art/bg-classroom");
    }

    public static Texture2D Icon()
    {
        return Load("Art/app-icon");
    }

    public static Texture2D Panel()
    {
        return Load("Art/ui-panel");
    }

    public static Texture2D BtnCoral()
    {
        return Load("Art/ui-btn-coral");
    }

    public static Texture2D BtnTeal()
    {
        return Load("Art/ui-btn-teal");
    }

    public static Texture2D Speech()
    {
        return Load("Art/ui-speech");
    }

    static Texture2D Load(string path)
    {
        if (Cache.ContainsKey(path)) return Cache[path];
        Texture2D texture = Resources.Load<Texture2D>(path);
        Cache[path] = texture;
        return texture;
    }
}
