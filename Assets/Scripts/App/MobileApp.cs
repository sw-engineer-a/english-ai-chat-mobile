using System;
using UnityEngine;
using UnityEngine.UI;

public class MobileApp : MonoBehaviour
{
    void Awake()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.88f, 0.78f, 1f);
            cam.stereoTargetEye = StereoTargetEyeMask.None;
        }
        ShowSplash();
        try
        {
            if (GetComponent<DragonApp>() == null) gameObject.AddComponent<DragonApp>();
        }
        catch (Exception error)
        {
            ShowError(error.Message);
        }
    }

    void ShowSplash()
    {
        Texture2D icon = Resources.Load<Texture2D>("Art/app-icon");
        Canvas canvas = NewCanvas("Splash");
        RectTransform plate = Panel(canvas.transform, Vector2.zero, Vector2.one, new Color(0.55f, 0.88f, 0.78f, 1f));
        if (icon == null) return;
        var go = new GameObject("Icon");
        go.transform.SetParent(plate, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 420f);
        var image = go.AddComponent<RawImage>();
        image.texture = icon;
        image.raycastTarget = false;
    }

    void ShowError(string message)
    {
        Canvas canvas = NewCanvas("StartupError");
        RectTransform plate = Panel(canvas.transform, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.7f), Color.white);
        var go = new GameObject("Error");
        go.transform.SetParent(plate, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 24f);
        rect.offsetMax = new Vector2(-24f, -24f);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 28;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.text = message;
    }

    static Canvas NewCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = name == "StartupError" ? 50 : 0;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform Panel(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject("Plate");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.AddComponent<Image>();
        image.color = color;
        return rect;
    }
}
