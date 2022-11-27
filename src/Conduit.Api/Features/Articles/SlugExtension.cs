using System.Text.RegularExpressions;

namespace Conduit.Api.Features.Articles
{
    public static class SlugExtension
    {
        public static string ToSlug(this string s)
        {
            s = s.ToLower();
            s = Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            s = Regex.Replace(s, @"\s", "-");
            return s;
        }
    }
}
