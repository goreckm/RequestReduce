using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RequestReduce.Utilities;
using System;
using RequestReduce.ResourceTypes;
using RequestReduce.Configuration;

namespace RequestReduce.Module
{
    public interface IResponseTransformer
    {
        string Transform(string preTransform);
    }

    public class ResponseTransformer : IResponseTransformer
    {
        private readonly IReductionRepository reductionRepository;
        private readonly IRRConfiguration config;
        private static readonly Regex UrlPattern = new Regex(@"(href|src)=""?(?<url>[^"" ]+)""?[^ />]+[ />]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly IReducingQueue reducingQueue;
        private readonly HttpContextBase context;

        public ResponseTransformer(IReductionRepository reductionRepository, IReducingQueue reducingQueue, HttpContextBase context, IRRConfiguration config)
        {
            this.reductionRepository = reductionRepository;
            this.reducingQueue = reducingQueue;
            this.context = context;
            this.config = config;
        }

        public string Transform(string preTransform)
        {
            if (!config.JavaScriptProcesingDisabled) preTransform = Transform<JavaScriptResource>(preTransform);
            if(!config.CssProcesingDisabled) preTransform = Transform<CssResource>(preTransform);

            return preTransform;
        }

        private string Transform<T>(string preTransform) where T : IResourceType, new()
        {
            var resource = new T();
            var matches = resource.ResourceRegex.Matches(preTransform);
            if (matches.Count > 0)
            {
                var urls = new StringBuilder();
                foreach (var match in matches)
                {
                    var urlMatch = UrlPattern.Match(match.ToString());
                    if (urlMatch.Success)
                    {
                        urls.Append(RelativeToAbsoluteUtility.ToAbsolute(context.Request.Url, urlMatch.Groups["url"].Value));
                        urls.Append("::");
                    }
                }
                RRTracer.Trace("Looking for reduction for {0}", urls);
                var transform = reductionRepository.FindReduction(urls.ToString());
                if (transform != null)
                {
                    RRTracer.Trace("Reduction found for {0}", urls);
                    var closeHeadIdx = preTransform.IndexOf('>');
                    preTransform = preTransform.Insert(closeHeadIdx + 1, resource.TransformedMarkupTag(transform));
                    foreach (var match in matches)
                        preTransform = preTransform.Replace(match.ToString(), "");
                    return preTransform;
                }
                reducingQueue.Enqueue(new QueueItem<T> { Urls = urls.ToString() });
                RRTracer.Trace("No reduction found for {0}. Enqueuing.", urls);
            }
            return preTransform;
        }
    }
}
