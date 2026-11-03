# Mobile (Unity Android)

Little Dragon / GOGO English conversation app. The website is the separate `english-ai-chat-web` repository.

Open this folder in **Unity 5.6.2f1** (not Unity 6):

`c:\Users\ASUS\Documents\company\english-ai-chat-mobile`

1. File → Open Scene → `Assets/Scenes/Main.unity`
2. File → Build Settings → switch platform to **Android**
3. Player Settings: Minimum API Level **21**, Target API Level **28**
4. Press Play. First serial: `123456789`

English lines ship as small `.pack` files under `Assets/StreamingAssets/english`. Rebuild them with `tools/pack-english.ps1` after regenerating `csv/english` in `english-ai-chat-web`.

Hold Start to speak on a phone. In the editor, type an English sentence and release Start.

Package name: `com.englishchat.mobile`. Unity 5.6 builds ARMv7 Android.

Art lives in `Assets/Resources/Art` (production dragon poses, helper character, classroom/park/night backgrounds, app icon). The conversation screen loads those images at runtime and plays idle bob + talk pulse + caption fade animations.
