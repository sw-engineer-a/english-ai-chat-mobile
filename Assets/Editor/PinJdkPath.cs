using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public class PinJdkPath
{
    const string Jdk = "C:/Users/ASUS/AppData/Local/jdk8/jdk8u482-b08";

    static PinJdkPath()
    {
        if (EditorPrefs.GetString("JdkPath") != Jdk)
            Debug.Log("Android JDK for this Unity 5.6 project set to " + Jdk);
        EditorPrefs.SetString("JdkPath", Jdk);

        if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.Mono2x)
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            Debug.Log("Android scripting backend set to Mono. This build does not use the NDK.");
        }

        PlayerSettings.virtualRealitySupported = false;
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.MultiPass;
        PlayerSettings.mobileMTRendering = false;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        GraphicsDeviceType[] apis = new GraphicsDeviceType[1];
        apis[0] = GraphicsDeviceType.OpenGLES2;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, apis);
        PlayerSettings.use32BitDisplayBuffer = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        ApplyBrand();
    }

    static void ApplyBrand()
    {
        const string path = "Assets/Resources/Art/app-icon.jpg";
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (icon == null) return;

        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
        Texture2D[] icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = icon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);

        Texture2D[] shared = new Texture2D[1];
        shared[0] = icon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, shared);

        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = true;
        PlayerSettings.SplashScreen.backgroundColor = new Color(0.55f, 0.88f, 0.78f, 1f);
        Sprite logo = LoadSprite(path);
        if (logo == null) return;
        PlayerSettings.SplashScreenLogo[] logos = new PlayerSettings.SplashScreenLogo[1];
        logos[0] = PlayerSettings.SplashScreenLogo.Create(2.5f, logo);
        PlayerSettings.SplashScreen.logos = logos;
    }

    static Sprite LoadSprite(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null) return sprite;
        }
        return null;
    }
}
