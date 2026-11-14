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

        /// <summary>
        /// When the story stands on the visitor's own table, their room is drawn behind the eye buffer and shows through
        /// wherever the buffer's alpha is zero. A shader that adds light must therefore leave alpha alone, or its whole
        /// card blacks out the room. Only a headset shows this, so it is held here.
        /// </summary>
        [Test]
        public void TheStoryTablesFireAddsLightWithoutCoveringTheRoomBehindIt()
        {
            var flame = File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Art/Shaders/Flame.shader"));
            Assert.That(flame, Does.Contain("Blend One One, Zero One"));
            Assert.That(flame, Does.Not.Match(@"Blend One One\s*\r?\n"), "no pass adds to alpha");
        }

        /// <summary>
        /// The other surfaces of the story table are opaque or cut out: each writes full alpha, so the room never shows
        /// through the table, the stage or the page.
        /// </summary>
        [Test]
        public void TheStoryTablesSolidSurfacesCoverTheRoomBehindThem()
        {
            foreach (var name in new[] { "Illustration", "PaintedProp" })
            {
                var text = File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Art/Shaders/" + name + ".shader"));
                Assert.That(text, Does.Not.Match(@"(?m)^\s*Blend\s"), name + " is not blended");
                Assert.That(text, Does.Match(@"return fixed4\(.*, 1\);"), name + " writes full alpha");
            }
            // The plain surface shader takes its blend from the material; the table's materials are made opaque.
            var surface = File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Art/Shaders/GallerySurface.shader"));
            Assert.That(surface, Does.Contain("_Visibility);"));
            var builder = File.ReadAllText(RepositoryFile("unity/Assets/Sankofa/Editor/SankofaStoryTable.cs"));
            Assert.That(builder, Does.Contain("material.SetFloat(\"_Visibility\", 1); material.SetFloat(\"_SrcBlend\", 1); material.SetFloat(\"_DstBlend\", 0);"));
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
