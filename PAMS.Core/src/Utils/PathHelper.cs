namespace PAMS.Core.Utils;

public static class PathHelper
{
    public static string GetRelativePath(string root, string full)
    {
        return Path.GetRelativePath(root, full);
    }

    public static string GetAbsolutePath(string root, string relative)
    {
        return Path.Combine(root, relative);
    }
}