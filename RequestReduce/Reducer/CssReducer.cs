﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RequestReduce.Store;
using RequestReduce.Utilities;
using RequestReduce.ResourceTypes;

namespace RequestReduce.Reducer
{
    public class CssReducer : HeadResourceReducerBase<CssResource>
    {
        private readonly ISpriteManager spriteManager;
        private readonly ICssImageTransformer cssImageTransformer;
        private static readonly Regex cssImportPattern = new Regex(@"@import[\s]+url[\s]*\([\s]*['""]?(?<url>[^'"" ]+)['""]?[\s]*\)[\s]*?;", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public CssReducer(IWebClientWrapper webClientWrapper, IStore store, IMinifier minifier, ISpriteManager spriteManager, ICssImageTransformer cssImageTransformer, IUriBuilder uriBuilder) : base(webClientWrapper, store, minifier, uriBuilder)
        {
            this.cssImageTransformer = cssImageTransformer;
            this.spriteManager = spriteManager;
        }

        protected override string ProcessResource(Guid key, IEnumerable<string> urls)
        {
            spriteManager.SpritedCssKey = key;
            var mergedCss = new StringBuilder();
            var imageUrls = new List<BackgroundImageClass>();
            foreach (var url in urls)
                mergedCss.Append(ProcessCss(url, imageUrls));
            foreach (var imageUrl in imageUrls)
                spriteManager.Add(imageUrl);
            spriteManager.Flush();
            return SpriteCss(mergedCss.ToString(), imageUrls);
        }

        protected virtual string ProcessCss(string url, List<BackgroundImageClass> imageUrls)
        {
            var cssContent = webClientWrapper.DownloadString<CssResource>(url);
            cssContent = ExpandImports(cssContent, url);
            imageUrls.AddRange(cssImageTransformer.ExtractImageUrls(ref cssContent, url));
            return cssContent;
        }

        private string ExpandImports(string cssContent, string parentUrl)
        {
            var imports = cssImportPattern.Matches(cssContent);
            foreach (Match match in imports)
            {
                var url = match.Groups["url"].Value;
                var absoluteUrl = RelativeToAbsoluteUtility.ToAbsolute(parentUrl, url);
                var importContent = webClientWrapper.DownloadString<CssResource>(absoluteUrl);
                importContent = ExpandImports(importContent, absoluteUrl);
                cssContent = cssContent.Replace(match.ToString(), importContent);
            }
            return cssContent;
        }

        protected virtual string SpriteCss(string css, List<BackgroundImageClass> imageUrls)
        {
            return spriteManager.Aggregate(css, (current, spritedImage) => cssImageTransformer.InjectSprite(current, spritedImage));
        }
    }
}
