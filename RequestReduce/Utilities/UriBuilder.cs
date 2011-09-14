using System;
using RequestReduce.Configuration;
using RequestReduce.Reducer;

namespace RequestReduce.Utilities
{
    public interface IUriBuilder
    {
        string BuildResourceUrl(Guid key, byte[] bytes, ResourceType resourceType);
        string BuildResourceUrl(Guid key, string signature, ResourceType resourceType);
        string BuildSpriteUrl(Guid key, byte[] bytes);
        string ParseFileName(string url);
        Guid ParseKey(string url);
        string ParseSignature(string url);
    }

    public class UriBuilder : IUriBuilder
    {
        private readonly IRRConfiguration configuration;
        public const string CssFileName = "RequestReducedStyle.css";
        public const string JsFileName = "RequestReducedScript.js";

        public UriBuilder(IRRConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string BuildResourceUrl(Guid key, byte[] bytes, ResourceType resourceType)
        {
            return BuildResourceUrl(key, Hasher.Hash(bytes).RemoveDashes(), resourceType);
        }

        public string BuildResourceUrl(Guid key, string signature, ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Css:
                    return string.Format("{0}{1}/{2}-{3}-{4}", configuration.ContentHost, configuration.SpriteVirtualPath, key.RemoveDashes(), signature, CssFileName);
                case ResourceType.JavaScript:
                    return string.Format("{0}{1}/{2}-{3}-{4}", configuration.ContentHost, configuration.SpriteVirtualPath, key.RemoveDashes(), signature, JsFileName);
                default:
                    throw new ArgumentException("Cannot Build Url for Resources of unknown type", "resourceType");
            }
        }

        public string BuildSpriteUrl(Guid key, byte[] bytes)
        {
            return string.Format("{0}{1}/{2}-{3}.png", configuration.ContentHost, configuration.SpriteVirtualPath, key.RemoveDashes(), Hasher.Hash(bytes).RemoveDashes());
        }

        public string ParseFileName(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1);
        }

        public Guid ParseKey(string url)
        {
            var idx = url.LastIndexOf('/');
            string keyDir = idx > -1 ? url.Substring(idx + 1) : url;
            string strKey = string.Empty;
            idx = keyDir.IndexOf('-');
            if (idx > -1)
                strKey = keyDir.Substring(0, idx);
            Guid key;
            Guid.TryParse(strKey, out key);
            return key;
        }

        public string ParseSignature(string url)
        {
            var idx = url.LastIndexOf('/');
            var keyDir = idx > -1 ? url.Substring(idx + 1) : url;
            var strKey = string.Empty;
            try
            {
                strKey = keyDir.Substring(33, 32);
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            Guid key;
            Guid.TryParse(strKey, out key);
            return key.RemoveDashes();
        }
    }
}
