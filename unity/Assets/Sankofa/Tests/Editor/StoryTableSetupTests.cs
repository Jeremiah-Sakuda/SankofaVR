using NUnit.Framework;
using Sankofa.EditorTools;

namespace Sankofa.Tests
{
    public sealed class StoryTableSetupTests
    {
        const string Begin = "<manifest xmlns:android='http://schemas.android.com/apk/res/android' xmlns:tools='http://schemas.android.com/tools'>";

        [Test]
        public void UpgradingOfflineManifestRemovesOnlyTheObsoleteNetworkSuppression()
        {
            var source = Begin + "<uses-permission tools:node='remove' android:name='android.permission.INTERNET' /><uses-permission tools:node='remove' android:name='android.permission.RECORD_AUDIO' /></manifest>";
            var upgraded = SankofaProjectSetup.PermitStoryNetworking(source);
            Assert.That(upgraded, Does.Not.Contain("android.permission.INTERNET"));
            Assert.That(upgraded, Does.Contain("android.permission.RECORD_AUDIO"));
            Assert.That(SankofaProjectSetup.PermitStoryNetworking(upgraded), Is.EqualTo(upgraded));
        }

        [Test]
        public void TheShippedManifestAsksNothingOfTheVisitorAtLaunch()
        {
            // Unity's player asks for every declared runtime permission as the app opens unless this is set. Meta's
            // manifest tool writes it as false, so a regenerated manifest must be corrected before a build.
            var manifest = System.IO.File.ReadAllText("Assets/Plugins/Android/AndroidManifest.xml");
            Assert.That(manifest, Does.Contain("android:name=\"unityplayer.SkipPermissionsDialog\" android:value=\"true\""),
                "the room scan and the microphone are asked for only when a visitor chooses the feature that needs them");
        }

        [Test]
        public void ExistingNetworkGrantAndOtherManifestDataRemainUnchanged()
        {
            var source = Begin + "<uses-permission android:name='android.permission.INTERNET' /><application android:label='Sankofa' /></manifest>";
            Assert.That(SankofaProjectSetup.PermitStoryNetworking(source), Is.EqualTo(source));
        }
    }
}
