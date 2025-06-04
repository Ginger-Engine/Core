namespace Engine.Core.Helper;

public class PathHelper
{
    public static string Normalize(string path)
    {
        if (path.StartsWith('/') || path.StartsWith('\\'))
            path = path[1..];
        
        path = path.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }
}