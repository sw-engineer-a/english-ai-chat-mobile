using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// On-device English recognition. The model is copied out of the APK once, then reused with no network.
/// </summary>
public class OfflineVoice
{
    IntPtr model;
    IntPtr recognizer;
    AudioClip micClip;
    float[] chunk = new float[1600];
    int lastPos;
    int rate = 16000;
    string committed = "";
    string partial = "";
    bool want;
    bool preparing;
    bool nativeReady;
    bool micFailed;
    int waitFrames = 2;
    readonly object gate = new object();

    public bool Ready;
    public bool Busy;
    public bool Failed;
    public bool Open;
    public float Level;
    public string Live = "";
    public string Status = "";

    public void Prepare()
    {
        if (preparing || Ready || Failed) return;
        preparing = true;
        Busy = true;
        Status = "Preparing offline speech…";
    }

    public void Begin()
    {
        want = true;
        micFailed = false;
        committed = "";
        partial = "";
        Live = "";
        Level = 0f;
        if (Failed) return;
        if (Ready) StartMic();
        else
        {
            Busy = true;
            Status = "Preparing offline speech…";
            Prepare();
        }
    }

    public void Tick()
    {
        if (preparing)
        {
            if (waitFrames > 0) waitFrames--;
            else Load();
        }
        if (want && Ready && recognizer == IntPtr.Zero && !Failed && !micFailed && !Open)
            StartMic();
        if (recognizer != IntPtr.Zero)
            Feed();
    }

    public string End()
    {
        want = false;
        string said = committed;
        if (recognizer != IntPtr.Zero)
        {
            string piece = ReadText(VoskNative.vosk_recognizer_final_result(recognizer), "text");
            if (piece.Length > 0) said = (said + " " + piece).Trim();
            VoskNative.vosk_recognizer_free(recognizer);
            recognizer = IntPtr.Zero;
        }
        StopMic();
        Live = said;
        Level = 0f;
        return said;
    }

    public void Cancel()
    {
        want = false;
        if (recognizer != IntPtr.Zero)
        {
            VoskNative.vosk_recognizer_free(recognizer);
            recognizer = IntPtr.Zero;
        }
        StopMic();
        Level = 0f;
    }

    void Load()
    {
        try
        {
            LoadNative();
            string root = Application.persistentDataPath + "/vosk";
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass store = new AndroidJavaClass("com.englishchat.mobile.ModelStore");
            string path = store.CallStatic<string>("install", activity, "vosk/model.zip", root);
            IntPtr created = VoskNative.vosk_model_new(path);
            if (created == IntPtr.Zero)
                throw new Exception("Could not open the offline speech model.");
            lock (gate) model = created;
            Ready = true;
            Busy = false;
            Status = "Offline speech ready";
        }
        catch (Exception error)
        {
            Debug.LogWarning(error.Message);
            Failed = true;
            Busy = false;
            Status = error.Message;
            if (string.IsNullOrEmpty(Status)) Status = "Offline speech failed.";
            else if (Status.Length > 160) Status = Status.Substring(0, 160);
        }
        finally
        {
            preparing = false;
        }
    }

    void LoadNative()
    {
        if (nativeReady) return;
        AndroidJavaClass system = new AndroidJavaClass("java.lang.System");
        system.CallStatic("loadLibrary", "vosk");
        VoskNative.vosk_set_log_level(-1);
        nativeReady = true;
    }

    void StartMic()
    {
        if (Open || recognizer != IntPtr.Zero) return;
        IntPtr loaded;
        lock (gate) loaded = model;
        if (loaded == IntPtr.Zero) return;

        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
        rate = 16000;
        if (minFreq > 0 && rate < minFreq) rate = minFreq;
        if (maxFreq > 0 && rate > maxFreq) rate = maxFreq;

        micClip = Microphone.Start(null, true, 2, rate);
        if (micClip == null)
        {
            micFailed = true;
            Open = false;
            Status = "No mic audio. Check LDPlayer's microphone, or type.";
            return;
        }

        recognizer = VoskNative.vosk_recognizer_new(loaded, rate);
        if (recognizer == IntPtr.Zero)
        {
            StopMic();
            Failed = true;
            Status = "Offline speech failed. Type instead.";
            return;
        }

        lastPos = Microphone.GetPosition(null);
        if (lastPos < 0) lastPos = 0;
        Open = true;
        Busy = false;
        Status = "Mic on — speak";
    }

    void StopMic()
    {
        if (micClip != null)
        {
            Microphone.End(null);
            micClip = null;
        }
        Open = false;
    }

    void Feed()
    {
        if (micClip == null) return;
        int pos = Microphone.GetPosition(null);
        if (pos < 0 || micClip.samples <= 0) return;
        int samples = pos - lastPos;
        if (samples < 0) samples += micClip.samples;
        int minSamples = rate / 10;
        if (samples < minSamples) return;
        if (samples > rate) samples = rate;

        int start = pos - samples;
        if (start < 0) start += micClip.samples;
        if (chunk.Length < samples) chunk = new float[samples];

        if (start + samples <= micClip.samples)
        {
            float[] data = new float[samples];
            micClip.GetData(data, start);
            Accept(data, samples);
        }
        else
        {
            int first = micClip.samples - start;
            float[] data = new float[samples];
            float[] head = new float[first];
            float[] tail = new float[samples - first];
            micClip.GetData(head, start);
            micClip.GetData(tail, 0);
            Array.Copy(head, 0, data, 0, first);
            Array.Copy(tail, 0, data, first, tail.Length);
            Accept(data, samples);
        }
        lastPos = pos;
    }

    void Accept(float[] data, int samples)
    {
        float sum = 0f;
        float peak = 0f;
        for (int i = 0; i < samples; i++)
        {
            float a = data[i] < 0f ? -data[i] : data[i];
            if (a > peak) peak = a;
            sum += data[i] * data[i];
        }
        float rms = Mathf.Sqrt(sum / samples);
        Level = Mathf.Clamp01(Mathf.Max(rms * 8f, peak * 3f));

        int done = VoskNative.vosk_recognizer_accept_waveform_f(recognizer, data, samples);
        if (done == 1)
        {
            string piece = ReadText(VoskNative.vosk_recognizer_result(recognizer), "text");
            if (piece.Length > 0) committed = (committed + " " + piece).Trim();
            partial = "";
        }
        else
        {
            string piece = ReadText(VoskNative.vosk_recognizer_partial_result(recognizer), "partial");
            if (piece.Length > 0) partial = piece;
        }
        Live = partial.Length > 0 ? (committed + " " + partial).Trim() : committed;
    }

    static string ReadText(IntPtr pointer, string key)
    {
        if (pointer == IntPtr.Zero) return "";
        string json = Marshal.PtrToStringAnsi(pointer);
        if (string.IsNullOrEmpty(json)) return "";
        string token = "\"" + key + "\"";
        int mark = json.IndexOf(token);
        if (mark < 0) return "";
        int colon = json.IndexOf(':', mark + token.Length);
        if (colon < 0) return "";
        int open = json.IndexOf('"', colon + 1);
        if (open < 0) return "";
        int close = json.IndexOf('"', open + 1);
        if (close < 0) return "";
        return json.Substring(open + 1, close - open - 1).Trim();
    }
}

static class VoskNative
{
    [DllImport("vosk")]
    public static extern void vosk_set_log_level(int level);

    [DllImport("vosk")]
    public static extern IntPtr vosk_model_new(string model_path);

    [DllImport("vosk")]
    public static extern IntPtr vosk_recognizer_new(IntPtr model, float sample_rate);

    [DllImport("vosk")]
    public static extern void vosk_recognizer_free(IntPtr recognizer);

    [DllImport("vosk")]
    public static extern int vosk_recognizer_accept_waveform_f(IntPtr recognizer, float[] data, int length);

    [DllImport("vosk")]
    public static extern IntPtr vosk_recognizer_result(IntPtr recognizer);

    [DllImport("vosk")]
    public static extern IntPtr vosk_recognizer_partial_result(IntPtr recognizer);

    [DllImport("vosk")]
    public static extern IntPtr vosk_recognizer_final_result(IntPtr recognizer);
}
