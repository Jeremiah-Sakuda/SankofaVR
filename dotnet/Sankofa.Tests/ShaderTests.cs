using System.IO;
using NUnit.Framework;
using static Sankofa.Tests.Fixtures;

namespace Sankofa.Tests
{
    /// <summary>
    /// Quest renders both eyes in one pass. A custom shader without Unity's stereo macros can draw an object
    /// incorrectly in one eye, which only shows on a headset. Checked here so CI catches it without Unity.
    /// </summary>
    public class ShaderTests
    {
        [Test]
        public void EverySankofaShaderIsSinglePassStereoSafe()
        {
            var shaders = Directory.GetFiles(RepositoryFile("unity/Assets/Sankofa/Art/Shaders"), "*.shader");
            Assert.That(shaders, Is.Not.Empty);
            foreach (var path in shaders)
            {
                var text = WithIncludes(path);
                foreach (var macro in new[]
                         {
                             "UNITY_VERTEX_INPUT_INSTANCE_ID", "UNITY_VERTEX_OUTPUT_STEREO", "UNITY_SETUP_INSTANCE_ID",
                             "UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO", "UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX", "multi_compile_instancing",
                         })
                    Assert.That(text, Does.Contain(macro), $"{Path.GetFileName(path)} lacks {macro}");
            }
        }

        /// <summary>A shader's text with the project's own include files beside it inlined, as the compiler sees it.</summary>
        static string WithIncludes(string path)
        {
            var text = File.ReadAllText(path);
            foreach (var include in Directory.GetFiles(Path.GetDirectoryName(path)!, "*.cginc"))
                if (text.Contains("#include \"" + Path.GetFileName(include) + "\""))
                    text += File.ReadAllText(include);
            return text;
        }
    }
}
