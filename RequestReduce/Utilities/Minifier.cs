using Microsoft.Ajax.Utilities;
using RequestReduce.Reducer;
using System;

namespace RequestReduce.Utilities
{
    public class Minifier : IMinifier
    {
        private Microsoft.Ajax.Utilities.Minifier minifier = new Microsoft.Ajax.Utilities.Minifier();

        public string Minify(string unMinifiedContent, ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Css:
                    return minifier.MinifyStyleSheet(unMinifiedContent);
                case ResourceType.JavaScript:
                    return minifier.MinifyJavaScript(unMinifiedContent);
                default:
                    throw new ArgumentException("Cannot Minify Resources of unknown type", "resourceType");
            }
        }

    }
}