// Stand-ins for Unity's test framework, for type-checking only.
using System;
namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public class UnityTestAttribute : NUnit.Framework.TestAttribute { }
    [AttributeUsage(AttributeTargets.Method)] public class UnitySetUpAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class UnityTearDownAttribute : Attribute { }
    public static class LogAssert
    {
        public static bool ignoreFailingMessages { get; set; }
        public static void Expect(UnityEngine.LogType type, string message) { }
        public static void Expect(UnityEngine.LogType type, System.Text.RegularExpressions.Regex message) { }
        public static void NoUnexpectedReceived() { }
    }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static UnityEngine.AsyncOperation LoadSceneAsyncInPlayMode(string path, UnityEngine.SceneManagement.LoadSceneParameters parameters) => null;
        public static UnityEngine.SceneManagement.Scene LoadSceneInPlayMode(string path, UnityEngine.SceneManagement.LoadSceneParameters parameters) => default;
    }
}
