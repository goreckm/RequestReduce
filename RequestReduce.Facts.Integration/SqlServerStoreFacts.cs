﻿using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Moq;
using RequestReduce.Configuration;
using RequestReduce.Store;
using RequestReduce.Utilities;
using Xunit;
using TimeoutException = Xunit.Sdk.TimeoutException;
using UriBuilder = RequestReduce.Utilities.UriBuilder;

namespace RequestReduce.Facts.Integration
{
    public class SqlServerStoreFacts
    {
        private readonly IRRConfiguration config;
        private readonly IFileRepository repo;
        private readonly UriBuilder uriBuilder;
        private readonly string rrFolder;

        public SqlServerStoreFacts()
        {
            var dataDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName +
                          "\\RequestReduce.SampleWeb\\App_Data";
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            Database.DefaultConnectionFactory = new SqlCeConnectionFactory("System.Data.SqlServerCe.4.0");
            var mockConfig = new Mock<IRRConfiguration>();
            mockConfig.Setup(x => x.ConnectionStringName).Returns("data source=" + dataDir + "\\RequestReduce.sdf");
            config = mockConfig.Object;
            repo = new FileRepository(config);
            repo.Context.Database.Delete();
            rrFolder = IntegrationFactHelper.ResetPhysicalContentDirectoryAndConfigureStore(Configuration.Store.SqlServerStore);
            uriBuilder = new UriBuilder(config);
        }

        [OutputTraceOnFailFact]
        public void WillReduceToOneCss()
        {
            var cssPattern = new Regex(@"<link[^>]+type=""?text/css""?[^>]+>", RegexOptions.IgnoreCase);
            new WebClient().DownloadString("http://localhost:8877/Local.html");
            WaitToCreateCss();

            var response = new WebClient().DownloadString("http://localhost:8877/Local.html");

            Assert.Equal(1, cssPattern.Matches(response).Count);
        }

        [OutputTraceOnFailFact]
        public void WillUseSameReductionAfterAppPoolRecycle()
        {
            var cssPattern = new Regex(@"<link[^>]+type=""?text/css""?[^>]+>", RegexOptions.IgnoreCase);
            var urlPattern = new Regex(@"href=""?(?<url>[^"" ]+)""?[^ />]+[ />]", RegexOptions.IgnoreCase);
            new WebClient().DownloadString("http://localhost:8877/Local.html");
            WaitToCreateCss();
            var response = new WebClient().DownloadString("http://localhost:8877/Local.html");
            var css = cssPattern.Match(response).ToString();
            var url = urlPattern.Match(css).Groups["url"].Value;
            var fileName = uriBuilder.ParseFileName(url);
            var key = uriBuilder.ParseKey(url);
            var id = Hasher.Hash(key + fileName);
            var createTime = repo[id].LastUpdated;

            IntegrationFactHelper.RecyclePool();
            new WebClient().DownloadString("http://localhost:8877/Local.html");
            WaitToCreateCss();

            Assert.Equal(createTime, repo[id].LastUpdated);
        }

        [OutputTraceOnFailFact]
        public void WillReReduceCssAfterFileIsRemovedFromDb()
        {
            var cssPattern = new Regex(@"<link[^>]+type=""?text/css""?[^>]+>", RegexOptions.IgnoreCase);
            var urlPattern = new Regex(@"href=""?(?<url>[^"" ]+)""?[^ />]+[ />]", RegexOptions.IgnoreCase);
            new WebClient().DownloadString("http://localhost:8877/Local.html");
            WaitToCreateCss();
            var response = new WebClient().DownloadString("http://localhost:8877/Local.html");
            var css = cssPattern.Match(response).ToString();
            var url = urlPattern.Match(css).Groups["url"].Value;
            var fileName = uriBuilder.ParseFileName(url);
            var key = uriBuilder.ParseKey(url);
            var id = Hasher.Hash(key + fileName);
            var createTime = repo[id].LastUpdated;

            repo.Context.Files.Remove(repo[id]);
            repo.Context.SaveChanges();
            IntegrationFactHelper.RecyclePool();
            new WebClient().DownloadString("http://localhost:8877/Local.html");
            WaitToCreateCss();
            new WebClient().DownloadString("http://localhost:8877/Local.html");

            Assert.True(createTime < repo[id].LastUpdated);
        }

        [OutputTraceOnFailFact]
        public void WillAccessContentFromFile()
        {
            var cssPattern = new Regex(@"<link[^>]+type=""?text/css""?[^>]+>", RegexOptions.IgnoreCase);
            var urlPattern = new Regex(@"href=""?(?<url>[^"" ]+)""?[^ />]+[ />]", RegexOptions.IgnoreCase);
            Guid id;
            string url;
            using (var client = new WebClient())
            {
                client.DownloadString("http://localhost:8877/Local.html");
                WaitToCreateCss();
                var response = client.DownloadString("http://localhost:8877/Local.html");
                var css = cssPattern.Match(response).ToString();
                url = urlPattern.Match(css).Groups["url"].Value;
                var fileName = uriBuilder.ParseFileName(url);
                var key = uriBuilder.ParseKey(url);
                id = Hasher.Hash(key + fileName);
            }
            repo.Context.Files.Remove(repo[id]);
            repo.Context.SaveChanges();

            var req = HttpWebRequest.Create("http://localhost:8877" + url);
            var response2 = req.GetResponse() as HttpWebResponse;

            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            response2.Close();
        }

        public void WillRecreateFileIfFileIsDeleted()
        {
            var cssPattern = new Regex(@"<link[^>]+type=""?text/css""?[^>]+>", RegexOptions.IgnoreCase);
            var urlPattern = new Regex(@"href=""?(?<url>[^"" ]+)""?[^ />]+[ />]", RegexOptions.IgnoreCase);
            string file;
            string url;
            using (var client = new WebClient())
            {
                client.DownloadString("http://localhost:8877/Local.html");
                WaitToCreateCss();
                var response = client.DownloadString("http://localhost:8877/Local.html");
                var css = cssPattern.Match(response).ToString();
                url = urlPattern.Match(css).Groups["url"].Value;
                file = url.Replace("/RRContent", rrFolder).Replace("/", "\\");
                File.Delete(file);
            }

            new WebClient().DownloadData("http://localhost:8877" + url);

            Assert.True(File.Exists(file));
        }

        private void WaitToCreateCss()
        {
            var watch = new Stopwatch();
            watch.Start();
            while (repo.AsQueryable().FirstOrDefault(x => x.FileName == UriBuilder.CssFileName) == null && watch.ElapsedMilliseconds < 10000)
                Thread.Sleep(0);
            if (watch.ElapsedMilliseconds >= 10000)
                throw new TimeoutException(10000);
            Thread.Sleep(100);
        }

    }
}
