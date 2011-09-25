using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RequestReduce.ResourceTypes
{
    public struct JavaScriptResource : IResourceType
    {
        private static readonly string scriptFormat = @"<script src=""{0}"" type=""text/javascript"" ></script>";
        private static readonly Regex ScriptPattern = new Regex(@"<script[^>]+src=['""]?.*?['""]?[^>]+>\s*?(</script>)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string FileName
        {
            get { return "RequestReducedScript.js"; }
        }

        public IEnumerable<string> SupportedMimeTypes
        {
            get { return new[] { "text/javascript", "application/javascript", "application/x-javascript" }; }
        }

        public string TransformedMarkupTag(string url)
        {
            return string.Format(scriptFormat, url);
        }

        public Regex ResourceRegex
        {
            get { return ScriptPattern; }
        }

    }
}
