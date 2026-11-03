using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class EnglishLine
{
    public string question;
    public string[] replies;
    public int topic;
    public int topic2;
    public int topic3;
}

public static class EnglishBank
{
    public const int MaxPerTopic = 40;

    static readonly string[] Packs =
    {
        "ages-03-05.pack",
        "ages-06-08.pack",
        "ages-09-10.pack",
        "ages-11-12.pack",
        "ages-13-15.pack",
        "ages-15-plus.pack"
    };

    static readonly HashSet<string> Stop = new HashSet<string>(new string[]
    {
        "the", "a", "an", "is", "are", "am", "to", "at", "in", "on", "of", "and", "or",
        "my", "your", "you", "it", "yes", "no", "ok", "okay", "maybe", "what", "how"
    });

    static string[] topicNames = new string[0];
    static List<EnglishLine>[] byTopic = new List<EnglishLine>[0];
    static readonly List<EnglishLine> all = new List<EnglishLine>();

    static bool ready;
    static bool busy;
    static int level;
    static string status = "English lines are not loaded.";

    public static bool Ready { get { return ready; } }
    public static bool Busy { get { return busy; } }
    public static int Level { get { return level; } }
    public static string Status { get { return status; } }
    public static string[] TopicNames { get { return topicNames; } }

    public static void Abort()
    {
        busy = false;
        if (!ready) status = "English lines are not loaded.";
    }

    public static bool LoadNow(int nextLevel)
    {
        nextLevel = Mathf.Clamp(nextLevel, 1, Packs.Length);
        if (ready && level == nextLevel && all.Count > 0) return true;

        busy = true;
        ready = false;
        level = nextLevel;
        all.Clear();
        topicNames = new string[0];
        byTopic = new List<EnglishLine>[0];
        status = "Opening English lines...";

        string fileName = Packs[nextLevel - 1];
        byte[] bytes = null;
        try
        {
            string path = Path.Combine(Path.Combine(Application.streamingAssetsPath, "english"), fileName);
            if (File.Exists(path)) bytes = File.ReadAllBytes(path);
        }
        catch (Exception error)
        {
            Debug.LogWarning(error.Message);
            bytes = null;
        }

        if (bytes == null || !Apply(bytes))
        {
            status = "Missing " + fileName;
            busy = false;
            ready = false;
            return false;
        }

        ready = all.Count > 0;
        busy = false;
        status = ready ? "English ready." : "English pack was empty.";
        return ready;
    }

    public static IEnumerator Load(int nextLevel)
    {
        nextLevel = Mathf.Clamp(nextLevel, 1, Packs.Length);
        if (ready && level == nextLevel && all.Count > 0) yield break;

        busy = true;
        ready = false;
        level = nextLevel;
        all.Clear();
        topicNames = new string[0];
        byTopic = new List<EnglishLine>[0];
        status = "Opening English lines...";

        string fileName = Packs[nextLevel - 1];
        byte[] bytes = null;
        yield return ReadPack(fileName, delegate(byte[] data) { bytes = data; });
        if (bytes == null || !Apply(bytes))
        {
            status = "Missing " + fileName;
            busy = false;
            ready = false;
            yield break;
        }

        ready = all.Count > 0;
        busy = false;
        status = ready ? "English ready." : "English pack was empty.";
    }

    public static bool TryAnswer(string user, out string answer, out int topic)
    {
        answer = "";
        topic = 1;
        if (!ready || all.Count == 0) return false;
        List<string> words = Words(user);
        if (words.Count == 0) return false;

        EnglishLine best = null;
        int bestScore = 0;
        bool bestIsReply = false;
        for (int i = 0; i < all.Count; i++)
        {
            EnglishLine line = all[i];
            int questionScore = Overlap(words, line.question);
            int replyScore = 0;
            if (line.replies != null)
            {
                for (int r = 0; r < line.replies.Length; r++)
                {
                    int score = Overlap(words, line.replies[r]);
                    if (score > replyScore) replyScore = score;
                }
            }
            int scoreTotal = questionScore + replyScore;
            if (scoreTotal <= bestScore) continue;
            bestScore = scoreTotal;
            best = line;
            bestIsReply = replyScore >= questionScore;
        }

        if (best == null || bestScore < 3) return false;
        topic = best.topic > 0 ? best.topic : 1;
        string full = Fullest(best.replies);
        answer = bestIsReply ? "Good. " + full : full;
        return !string.IsNullOrEmpty(answer);
    }

    public static string Question(int topic, string avoid)
    {
        if (!ready) return "What do you like?";
        List<EnglishLine> list = TopicList(topic);
        if (list == null || list.Count == 0) list = all;
        if (list == null || list.Count == 0) return "What do you like?";
        for (int n = 0; n < 8; n++)
        {
            EnglishLine line = list[UnityEngine.Random.Range(0, list.Count)];
            if (line.question != avoid && !string.IsNullOrEmpty(line.question)) return line.question;
        }
        return list[0].question;
    }

    public static string[] Script(int topic, int count)
    {
        List<EnglishLine> list = TopicList(topic);
        List<string> result = new List<string>();
        if (list == null) return result.ToArray();
        for (int i = 0; i < list.Count; i++)
        {
            if (string.IsNullOrEmpty(list[i].question)) continue;
            result.Add(list[i].question);
            if (result.Count >= count) break;
        }
        return result.ToArray();
    }

    public static string TopicName(int topic)
    {
        if (topic <= 0 || topic >= topicNames.Length) return "Topic";
        return string.IsNullOrEmpty(topicNames[topic]) ? "Topic" : topicNames[topic];
    }

    static List<EnglishLine> TopicList(int topic)
    {
        if (topic <= 0 || topic >= byTopic.Length) return null;
        List<EnglishLine> list = byTopic[topic];
        return list != null && list.Count > 0 ? list : null;
    }

    static IEnumerator ReadPack(string fileName, Action<byte[]> done)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string src = Application.streamingAssetsPath + "/english/" + fileName;
        WWW www = new WWW(src);
        yield return www;
        if (!string.IsNullOrEmpty(www.error))
        {
            done(null);
            yield break;
        }
        done(www.bytes);
#else
        string path = Path.Combine(Path.Combine(Application.streamingAssetsPath, "english"), fileName);
        done(File.Exists(path) ? File.ReadAllBytes(path) : null);
        yield break;
#endif
    }

    static bool Apply(byte[] bytes)
    {
        try
        {
            MemoryStream stream = new MemoryStream(bytes);
            BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
            byte[] magic = reader.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != (byte)'L' || magic[1] != (byte)'D' || magic[2] != (byte)'E' || magic[3] != (byte)'N')
            {
                reader.Close();
                stream.Close();
                return false;
            }
            reader.ReadUInt16();
            int topicCount = reader.ReadUInt16();
            List<string> names = new List<string>();
            names.Add("");
            for (int i = 0; i < topicCount; i++)
            {
                int id = reader.ReadUInt16();
                string name = ReadText(reader);
                while (names.Count <= id) names.Add("");
                names[id] = name;
            }
            int poolCount = reader.ReadUInt16();
            string[] pool = new string[poolCount];
            for (int i = 0; i < poolCount; i++) pool[i] = ReadText(reader);
            int lineCount = reader.ReadUInt16();
            topicNames = names.ToArray();
            byTopic = new List<EnglishLine>[Mathf.Max(topicNames.Length, 2)];
            for (int i = 0; i < byTopic.Length; i++) byTopic[i] = new List<EnglishLine>();
            for (int i = 0; i < lineCount; i++)
            {
                EnglishLine line = new EnglishLine();
                line.question = At(pool, reader.ReadUInt16());
                line.replies = new string[]
                {
                    At(pool, reader.ReadUInt16()),
                    At(pool, reader.ReadUInt16()),
                    At(pool, reader.ReadUInt16()),
                    At(pool, reader.ReadUInt16()),
                    At(pool, reader.ReadUInt16())
                };
                line.topic = reader.ReadUInt16();
                line.topic2 = reader.ReadUInt16();
                line.topic3 = reader.ReadUInt16();
                all.Add(line);
                AddBucket(line, line.topic);
                AddBucket(line, line.topic2);
                AddBucket(line, line.topic3);
            }
            reader.Close();
            stream.Close();
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning(error.Message);
            return false;
        }
    }

    static string ReadText(BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        if (length == 0) return "";
        return Encoding.UTF8.GetString(reader.ReadBytes(length));
    }

    static string At(string[] pool, int index)
    {
        if (index < 0 || index >= pool.Length) return "";
        return pool[index] != null ? pool[index] : "";
    }

    static void AddBucket(EnglishLine line, int topic)
    {
        if (topic <= 0 || topic >= byTopic.Length) return;
        if (byTopic[topic].Count < MaxPerTopic) byTopic[topic].Add(line);
    }

    static string Fullest(string[] replies)
    {
        string best = "";
        if (replies == null) return best;
        for (int i = 0; i < replies.Length; i++)
        {
            if (!string.IsNullOrEmpty(replies[i]) && replies[i].Length > best.Length) best = replies[i];
        }
        return best;
    }

    static int Overlap(List<string> words, string sentence)
    {
        if (string.IsNullOrEmpty(sentence) || words.Count == 0) return 0;
        string lower = sentence.ToLower();
        int score = 0;
        for (int i = 0; i < words.Count; i++)
        {
            if (lower.Contains(words[i])) score += words[i].Length;
        }
        return score;
    }

    static List<string> Words(string text)
    {
        List<string> list = new List<string>();
        StringBuilder word = new StringBuilder();
        string source = text != null ? text.ToLower() : "";
        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (char.IsLetter(ch)) word.Append(ch);
            else Flush(word, list);
        }
        Flush(word, list);
        return list;
    }

    static void Flush(StringBuilder word, List<string> list)
    {
        if (word.Length >= 3)
        {
            string token = word.ToString();
            if (!Stop.Contains(token)) list.Add(token);
        }
        word.Length = 0;
    }
}
