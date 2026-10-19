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
        public void ExistingNetworkGrantAndOtherManifestDataRemainUnchanged()
        {
            var source = Begin + "<uses-permission android:name='android.permission.INTERNET' /><application android:label='Sankofa' /></manifest>";
            Assert.That(SankofaProjectSetup.PermitStoryNetworking(source), Is.EqualTo(source));
        }
    }
}
