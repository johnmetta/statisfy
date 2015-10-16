using System.Text.RegularExpressions;
using Autofac;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Autofac;
using Nancy.Conventions;
using Nancy.Embedded.Conventions;
using Nancy.ViewEngines;
using NLog;
using Statsify.Aggregator.ComponentModel;
using Statsify.Aggregator.Configuration;
using Statsify.Aggregator.Http.Services;
using Statsify.Aggregator.Http.Services.Impl;
using Statsify.Core.Components.Impl;

namespace Statsify.Aggregator.Http
{
    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private readonly Logger httpLog = LogManager.GetLogger("Statsify.Aggregator.Http");

        public IMetricAggregator MetricAggregator { get; set; }

        protected override void ConfigureRequestContainer(ILifetimeScope container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);

            var builder = new ContainerBuilder();

            builder.Register(c => ConfigurationManager.Instance.Configuration).As<StatsifyAggregatorConfigurationSection>().SingleInstance();

            builder.RegisterType<MetricService>().As<IMetricService>().SingleInstance();
            builder.RegisterType<AnnotationService>().As<IAnnotationService>().SingleInstance();
            builder.Register(c => new MetricRegistry(c.Resolve<StatsifyAggregatorConfigurationSection>().Storage.Path)).AsImplementedInterfaces().SingleInstance();
            builder.Register(c => new AnnotationRegistry(c.Resolve<StatsifyAggregatorConfigurationSection>().Storage.Path)).AsImplementedInterfaces().SingleInstance();

            builder.RegisterInstance(MetricAggregator).As<IMetricAggregator>();

            builder.Update(container.ComponentRegistry);            
        }

        protected override void RequestStartup(ILifetimeScope container, IPipelines pipelines, NancyContext context)
        {
            pipelines.BeforeRequest.AddItemToEndOfPipeline(ctx =>
            {
                httpLog.Info("{0} {1}", ctx.Request.Method, ctx.Request.Path);
                return null;
            });
            pipelines.AfterRequest.
                AddItemToEndOfPipeline(ctx => {
                    ctx.Response.
                        WithHeader("Access-Control-Allow-Origin", "*").
                        WithHeader("Access-Control-Allow-Methods", "POST, GET").
                        WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-Type");
                });
        }

        protected override void ConfigureApplicationContainer(ILifetimeScope existingContainer)
        {
            base.ConfigureApplicationContainer(existingContainer);

            ResourceViewLocationProvider.RootNamespaces.Add(GetType().Assembly, "Statsify.Aggregator.Content.Views");
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get { return NancyInternalConfiguration.WithOverrides(ConfigurationBuilder); }
        }

        private static void ConfigurationBuilder(NancyInternalConfiguration x)
        {
            x.ViewLocationProvider = typeof(ResourceViewLocationProvider);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            nancyConventions.StaticContentsConventions.Add(
                EmbeddedStaticContentConventionBuilder.AddDirectory("Content", typeof(Bootstrapper).Assembly, "content"));
        }
    }
}



namespace Nancy.Embedded.Conventions
{
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Helpers;
    using Responses;

    public class EmbeddedStaticContentConventionBuilder
    {
        private static readonly ConcurrentDictionary<string, Func<Response>> ResponseFactoryCache;
        private static readonly Regex PathReplaceRegex = new Regex(@"[/\\]", RegexOptions.Compiled);

        static EmbeddedStaticContentConventionBuilder()
        {
            ResponseFactoryCache = new ConcurrentDictionary<string, Func<Response>>();
        }

        /// <summary>
        /// Adds a directory-based convention for embedded static convention.
        /// </summary>
        /// <param name="requestedPath">The path that should be matched with the request.</param>
        /// <param name="contentPath">The path to where the content is stored in your application, relative to the root. If this is <see langword="null" /> then it will be the same as <paramref name="requestedPath"/>.</param>
        /// <param name="allowedExtensions">A list of extensions that is valid for the conventions. If not supplied, all extensions are valid.</param>
        /// <returns>A <see cref="EmbeddedFileResponse"/> instance for the requested embedded static contents if it was found, otherwise <see langword="null"/>.</returns>
        public static Func<NancyContext, string, Response> AddDirectory(string requestedPath, Assembly assembly, string contentPath = null, params string[] allowedExtensions)
        {
            if (!requestedPath.StartsWith("/"))
            {
                requestedPath = string.Concat("/", requestedPath);
            }

            return (ctx, root) =>
            {
                var path =
                    HttpUtility.UrlDecode(ctx.Request.Path);

                var fileName =
                    Path.GetFileName(path);

                if (string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                var pathWithoutFilename =
                    GetPathWithoutFilename(fileName, path);

                if (!pathWithoutFilename.StartsWith(requestedPath, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] The requested resource '", path, "' does not match convention mapped to '", requestedPath, "'")));
                    return null;
                }

                contentPath =
                    GetContentPath(requestedPath, contentPath);

                var responseFactory =
                    ResponseFactoryCache.GetOrAdd(path, BuildContentDelegate(ctx, requestedPath, contentPath, assembly, allowedExtensions));

                return responseFactory.Invoke();
            };
        }

        private static Func<string, Func<Response>> BuildContentDelegate(NancyContext context, string requestedPath, string contentPath, Assembly assembly, string[] allowedExtensions)
        {
            return requestPath =>
            {
                context.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] Attempting to resolve embedded static content '", requestPath, "'")));

                var extension = Path.GetExtension(requestPath);

                if (allowedExtensions.Length != 0 && !allowedExtensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] The requested extension '", extension, "' does not match any of the valid extensions for the convention '", string.Join(",", allowedExtensions), "'")));
                    return () => null;
                }

                var transformedRequestPath =
                     GetSafeRequestPath(requestPath, requestedPath, contentPath);

                transformedRequestPath =
                    GetEncodedPath(transformedRequestPath);

                // Resolve relative paths by using c:\ as a fake root path
                var fileName =
                    Path.GetFullPath(Path.Combine("c:\\", transformedRequestPath));

                var contentRootPath =
                    Path.GetFullPath(Path.Combine("c:\\", GetEncodedPath(contentPath)));

                if (!IsWithinContentFolder(contentRootPath, fileName))
                {
                    context.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] The request '", fileName, "' is trying to access a path outside the content folder '", contentPath, "'")));
                    return () => null;
                }

                var resourceName =
                    Path.GetDirectoryName(assembly.GetName().Name + fileName.Substring(2)).Replace('\\', '.').Replace('-', '_');

                fileName =
                    Path.GetFileName(fileName);

                if (!assembly.GetManifestResourceNames().Any(x => string.Equals(x, resourceName + "." + fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] The requested resource '", requestPath, "' was not found in assembly '", assembly.GetName().Name, "'")));
                    return () => null;
                }

                context.Trace.TraceLog.WriteLog(x => x.AppendLine(string.Concat("[EmbeddedStaticContentConventionBuilder] Returning file '", fileName, "'")));
                return () => new EmbeddedFileResponse2(assembly, resourceName, fileName);
            };
        }

        private static string GetEncodedPath(string path)
        {
            return PathReplaceRegex.Replace(path.TrimStart(new[] { '/' }), Path.DirectorySeparatorChar.ToString());
        }

        private static string GetSafeRequestPath(string requestPath, string requestedPath, string contentPath)
        {
            var actualContentPath =
                (contentPath.Equals("/") ? string.Empty : contentPath);

            if (requestedPath.Equals("/"))
            {
                return string.Concat(actualContentPath, requestPath);
            }

            var expression =
                new Regex(Regex.Escape(requestedPath), RegexOptions.IgnoreCase);

            return expression.Replace(requestPath, actualContentPath, 1);
        }

        private static string GetContentPath(string requestedPath, string contentPath)
        {
            contentPath =
                contentPath ?? requestedPath;

            if (!contentPath.StartsWith("/"))
            {
                contentPath = string.Concat("/", contentPath);
            }

            return contentPath;
        }

        private static string GetPathWithoutFilename(string fileName, string path)
        {
            var pathWithoutFileName =
                path.Replace(fileName, string.Empty);

            return (pathWithoutFileName.Equals("/")) ?
                pathWithoutFileName :
                pathWithoutFileName.TrimEnd(new[] { '/' });
        }

        /// <summary>
        /// Returns whether the given filename is contained within the content folder
        /// </summary>
        /// <param name="contentRootPath">Content root path</param>
        /// <param name="fileName">Filename requested</param>
        /// <returns>True if contained within the content root, false otherwise</returns>
        private static bool IsWithinContentFolder(string contentRootPath, string fileName)
        {
            return fileName.StartsWith(contentRootPath, StringComparison.Ordinal);
        }
    }
}

namespace Nancy.Responses
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;

    public class EmbeddedFileResponse2 : Response
    {
        private static readonly byte[] ErrorText;

        static EmbeddedFileResponse2()
        {
            ErrorText = Encoding.UTF8.GetBytes("NOT FOUND");
        }

        public EmbeddedFileResponse2(Assembly assembly, string resourcePath, string name)
        {
            this.ContentType = MimeTypes.GetMimeType(name);
            this.StatusCode = HttpStatusCode.OK;

            var content =
                    GetResourceContent(assembly, resourcePath, name);

            if (content != null)
            {
                this.WithHeader("ETag", GenerateETag(content));
                content.Seek(0, SeekOrigin.Begin);
            }

            this.Contents = stream =>
            {
                if (content != null)
                {
                    content.CopyTo(stream);
                }
                else
                {
                    stream.Write(ErrorText, 0, ErrorText.Length);
                }
            };
        }

        private Stream GetResourceContent(Assembly assembly, string resourcePath, string name)
        {
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(x => GetFileNameFromResourceName(resourcePath, x).Equals(name, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
                return null;

            return assembly.GetManifestResourceStream(resourceName);
        }

        private static string GetFileNameFromResourceName(string resourcePath, string resourceName)
        {
            return Regex.Replace(resourceName, resourcePath, string.Empty, RegexOptions.IgnoreCase).TrimStart('.');
        }

        private static string GenerateETag(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return string.Concat("\"", ByteArrayToString(hash), "\"");
            }
        }

        private static string ByteArrayToString(byte[] data)
        {
            var output = new StringBuilder(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                output.Append(data[i].ToString("X2"));
            }

            return output.ToString();
        }
    }
}