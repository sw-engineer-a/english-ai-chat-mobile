using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class HistoryRow
{
    public string date;
    public int conversations;
    public int score;
}

[Serializable]
public class PurchaseRow
{
    public string date;
    public int diamonds;
    public string item;
}

[Serializable]
public class ProfileData
{
    public string name = "";
    public string sex = "M";
    public string dob = "";
    public string speakerSex = "F";
    public string characterId = "dragon";
    public string backgroundId = "classroom";
    public int level = 1;
    public bool caption = true;
    public int diamonds = 50;
    public int score = 1;
    public int mood = 50;
    public int strength = 50;
    public int talks;
    public int talksToday;
    public int topScoreToday;
    public string today = "";
    public string lastStatReset = "";
    public int sameQuestionCount;
    public string lastQuestion = "";
    public List<string> ownedItems = new List<string>();
    public List<string> wornItems = new List<string>();
    public List<string> ownedCharacters = new List<string>();
    public List<string> ownedBackgrounds = new List<string>();
    public List<HistoryRow> history = new List<HistoryRow>();
    public List<PurchaseRow> purchases = new List<PurchaseRow>();
}

public static class SaveGame
{
    const string SerialPassword = "123456789";
    public const string DiamondPassword = "1234567890";

    // Simple scramble — Unity 5.6 Mono rejects common AES key sizes.
    static readonly byte[] Key = Encoding.UTF8.GetBytes("LittleDragonGOGO1");

    public static string ProfilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "profile.json"); }
    }

    public static string SerialPath
    {
        get { return Path.Combine(Application.persistentDataPath, "serial.dat"); }
    }

    public static ProfileData LoadProfile()
    {
        try
        {
            if (!File.Exists(ProfilePath)) return Normalize(new ProfileData());
            var data = JsonUtility.FromJson<ProfileData>(File.ReadAllText(ProfilePath));
            return Normalize(data != null ? data : new ProfileData());
        }
        catch
        {
            return Normalize(new ProfileData());
        }
    }

    public static void SaveProfile(ProfileData data)
    {
        File.WriteAllText(ProfilePath, JsonUtility.ToJson(Normalize(data)));
    }

    static ProfileData Normalize(ProfileData data)
    {
        if (data == null) data = new ProfileData();
        if (data.name == null) data.name = "";
        if (data.sex == null) data.sex = "M";
        if (data.dob == null) data.dob = "";
        if (data.speakerSex == null) data.speakerSex = "F";
        if (data.characterId == null) data.characterId = "dragon";
        if (data.backgroundId == null) data.backgroundId = "classroom";
        if (data.today == null) data.today = "";
        if (data.lastStatReset == null) data.lastStatReset = "";
        if (data.lastQuestion == null) data.lastQuestion = "";
        if (data.ownedItems == null) data.ownedItems = new List<string>();
        if (data.wornItems == null) data.wornItems = new List<string>();
        if (data.ownedCharacters == null) data.ownedCharacters = new List<string>();
        if (data.ownedBackgrounds == null) data.ownedBackgrounds = new List<string>();
        if (data.history == null) data.history = new List<HistoryRow>();
        if (data.purchases == null) data.purchases = new List<PurchaseRow>();
        if (data.ownedCharacters.Count == 0) data.ownedCharacters.Add("dragon");
        if (data.ownedBackgrounds.Count == 0) data.ownedBackgrounds.Add("classroom");
        if (data.level < 1 || data.level > 6) data.level = 1;
        data.score = Mathf.Clamp(data.score, 0, 100);
        data.mood = Mathf.Max(0, data.mood);
        data.strength = Mathf.Max(0, data.strength);
        data.diamonds = Mathf.Max(0, data.diamonds);
        return data;
    }

    public static bool HasSerial()
    {
        return ReadSerial() == SerialPassword;
    }

    public static bool TrySaveSerial(string serial, string imei)
    {
        if (serial == null) serial = "";
        if (imei == null) imei = "";
        if (serial.Trim() != SerialPassword) return false;
        try
        {
            var plain = SerialPassword + "|" + imei.Trim();
            File.WriteAllText(SerialPath, Convert.ToBase64String(Scramble(Encoding.UTF8.GetBytes(plain))), Encoding.UTF8);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning(error.Message);
            return false;
        }
    }

    public static string ReadSerial()
    {
        try
        {
            if (!File.Exists(SerialPath)) return "";
            byte[] raw = File.ReadAllBytes(SerialPath);
            string text = Encoding.UTF8.GetString(raw);
            // New format: base64 scramble. Old AES files are ignored.
            if (text.IndexOf('|') >= 0) return "";
            byte[] plainBytes = Scramble(Convert.FromBase64String(text.Trim()));
            string plain = Encoding.UTF8.GetString(plainBytes);
            int bar = plain.IndexOf('|');
            return bar < 0 ? plain : plain.Substring(0, bar);
        }
        catch
        {
            return "";
        }
    }

    static byte[] Scramble(byte[] data)
    {
        byte[] output = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            output[i] = (byte)(data[i] ^ Key[i % Key.Length]);
        }
        return output;
    }
}
