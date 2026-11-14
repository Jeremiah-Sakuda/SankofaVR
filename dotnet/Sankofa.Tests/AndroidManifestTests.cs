using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace Sankofa.Tests
{
    /// <summary>
    /// The headset build's manifest, read as text so the check runs without Unity. What a visitor is asked on first
    /// launch only shows on a headset, so the settings that decide it are held here.
    /// </summary>
    public sealed class AndroidManifestTests
    {
        static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";
        static XDocument Manifest() => XDocument.Parse(File.ReadAllText(Fixtures.RepositoryFile("unity/Assets/Plugins/Android/AndroidManifest.xml")));

        [Test]
        public void NothingIsAskedOfTheVisitorAtLaunch()
        {
            // Unity's player asks for every declared runtime permission as the app opens unless this is set. The app asks
            // for each one itself, at the moment a visitor chooses the feature that needs it: the room scan when they
            // choose their own table, the microphone when they agree to ask aloud.
            var skip = Manifest().Descendants("meta-data").Single(m => (string)m.Attribute(Android + "name") == "unityplayer.SkipPermissionsDialog");
            Assert.That((string)skip.Attribute(Android + "value"), Is.EqualTo("true"));
        }

        [Test]
        public void HandsAreTheDeclaredWayInAndNoControllerIsRequired()
        {
            var manifest = Manifest();
            var features = manifest.Descendants("uses-feature").ToDictionary(f => (string)f.Attribute(Android + "name"), f => (string)f.Attribute(Android + "required"));
            Assert.That(features.ContainsKey("oculus.software.handtracking"), Is.True, "hand tracking is declared");
            Assert.That(features["com.oculus.feature.PASSTHROUGH"], Is.EqualTo("false"), "the room is optional: the story stands without it");
            var permissions = manifest.Descendants("uses-permission").Select(p => (string)p.Attribute(Android + "name")).ToArray();
            Assert.That(permissions, Does.Contain("com.oculus.permission.HAND_TRACKING"));
            Assert.That(permissions, Does.Contain("com.oculus.permission.USE_SCENE"), "the room scan is asked for only when a visitor chooses their own table");
        }
    }
}
