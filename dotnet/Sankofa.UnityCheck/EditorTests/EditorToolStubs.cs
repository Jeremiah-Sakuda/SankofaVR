// Stand-in for the one editor tool the story-table setup tests call; the real one lives in Sankofa.Editor.
namespace Sankofa.EditorTools
{
    public static class SankofaProjectSetup
    {
        public static string PermitStoryNetworking(string manifest) => manifest;
    }
}
