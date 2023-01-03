 #if UNITY_EDITOR
 using UnityEditor;
 using UnityEditor.Build;
 class MyCustomBuildProcessor : IPreprocessBuild
 {
      public int callbackOrder { get { return 0; } }
      public void OnPreprocessBuild(BuildTarget target, string path)
      {
          var load = ProjectSettings.LoadProjectSettings();
          PlayerSettings.keystorePass = load.keystorePassword;
          PlayerSettings.keyaliasPass = load.keystorePassword;
      // Do the preprocessing here
      }
 }
 #endif