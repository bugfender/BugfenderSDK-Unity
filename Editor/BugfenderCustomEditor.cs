using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Bugfender))]
[CanEditMultipleObjects]
public class BugfenderCustomEditor : Editor
{
    private SerializedProperty m_AppKey;
    private SerializedProperty m_EnableUIEventLogging;
    private SerializedProperty m_EnableCrashReporting;
    private SerializedProperty m_HideDeviceName;
    private SerializedProperty m_PrintToConsole;
    private SerializedProperty m_ApiURL;
    private SerializedProperty m_BaseURL;
    private static GUIStyle m_HeaderStyle;
    private static Texture2D m_Logo;

    void OnEnable()
    {
        m_AppKey = serializedObject.FindProperty("APP_KEY");
        m_EnableUIEventLogging = serializedObject.FindProperty("ENABLE_UI_EVENT_LOGGING");
        m_EnableCrashReporting = serializedObject.FindProperty("ENABLE_CRASH_REPORTING");
        m_HideDeviceName = serializedObject.FindProperty("HIDE_DEVICE_NAME");
        m_PrintToConsole = serializedObject.FindProperty("PRINT_TO_CONSOLE");
        m_ApiURL = serializedObject.FindProperty("API_URL");
        m_BaseURL = serializedObject.FindProperty("BASE_URL");
        m_HeaderStyle = new GUIStyle(GUIStyle.none)
        {
            fontStyle = FontStyle.Bold
        };
        m_Logo = Resources.Load<Texture2D>("BugfenderLogo");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(new GUIContent(m_Logo));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Bugfender Settings", m_HeaderStyle);
        EditorGUILayout.PropertyField(m_AppKey, new GUIContent("App Key"));
        EditorGUILayout.PropertyField(m_EnableUIEventLogging, new GUIContent("Enable UI Event Logging"));
        EditorGUILayout.PropertyField(m_EnableCrashReporting, new GUIContent("Enable Crash Reporting"));
        EditorGUILayout.PropertyField(m_HideDeviceName, new GUIContent("Hide device name"));
        EditorGUILayout.PropertyField(m_PrintToConsole, new GUIContent("Print to console"));
        EditorGUILayout.PropertyField(m_ApiURL, new GUIContent("API URL (optional)"));
        EditorGUILayout.PropertyField(m_BaseURL, new GUIContent("Base URL (optional)"));

        GUILayout.Space(30f);
        EditorGUILayout.LabelField("How to use Bugfender:", m_HeaderStyle);
        EditorGUILayout.SelectableLabel("Bugfender.Log(\"BF Initialized\");");
        EditorGUILayout.SelectableLabel("Bugfender.SetDeviceString(\"key\", \"value\");");
        EditorGUILayout.SelectableLabel("Bugfender.SetDeviceString(\"key2\", \"will remove\");");
        EditorGUILayout.SelectableLabel("Bugfender.RemoveDeviceKey(\"key2\");");
        EditorGUILayout.SelectableLabel("Bugfender.SendIssue(\"test\", \"this is a test\");");
        EditorGUILayout.SelectableLabel("Utils.ForceCrash(ForcedCrashCategory.Abort);");
        if (GUILayout.Button("Documentation", GUILayout.ExpandWidth(true)))
        {
            Application.OpenURL("https://support.bugfender.com/en/articles/118148-can-i-use-bugfender-with-unity");
        }
        
        serializedObject.ApplyModifiedProperties();
        //Repaint(); // uncomment while editing this file
    }

}