using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Dropsonic.AspNetCore.Routing.Tests
{
    // ReSharper disable once UnusedMember.Global
    partial class ProducesMatcherPolicyTests
    {
        private static class Make
        {
            public static EndpointBuilder Endpoint() => new();

            public static IOptions<MvcOptions> DefaultMvcOptions() => MvcOptions(_ => { });
            
            public static IOptions<MvcOptions> MvcOptions(Action<MvcOptions> setupAction)
            {
                var options = new MvcOptions();
                setupAction(options);
                return new OptionsWrapper<MvcOptions>(options);
            }

            public static IOptions<ProducesMatcherOptions> DefaultProducesMatcherOptions() =>
                ProducesMatcherOptions(_ => { });

            public static IOptions<ProducesMatcherOptions> ProducesMatcherOptions(Action<ProducesMatcherOptions> setupAction)
            {
                var options = new ProducesMatcherOptions();
                setupAction(options);
                return new OptionsWrapper<ProducesMatcherOptions>(options);
            }

            public static HttpContextBuilder HttpContext() => new();
        }

        private class HttpContextBuilder
        {
            private bool _hasAccept;
            private string _accept;
            private readonly List<(string Name, string Value)> _queryStringParameters = new();

            public static implicit operator HttpContext(HttpContextBuilder builder) => builder.Build();

            private HttpContext Build()
            {
                var httpContext = new DefaultHttpContext();

                if (_hasAccept)
                {
                    httpContext.Request.Headers[HeaderNames.Accept] = _accept;

                    foreach (var (name, value) in _queryStringParameters)
                    {
                        httpContext.Request.QueryString = httpContext.Request.QueryString.Add(name, value);
                    }
                }

                return httpContext;
            }

            public HttpContextBuilder WithAcceptHeader(string accept)
            {
                _hasAccept = true;
                _accept = accept;
                return this;
            }

            public HttpContextBuilder WithQueryStringParameter(string parameter, string value)
            {
                _queryStringParameters.Add((parameter, value));
                return this;
            }
        }

        private class EndpointBuilder
        {
            private readonly List<string> _contentTypes = new();
            private bool _shouldHaveEmptyProducesMetadata;

            public static implicit operator Endpoint(EndpointBuilder builder) => builder.Build();

            private Endpoint Build()
            {
                var metadata = EndpointMetadataCollection.Empty;

                if (_shouldHaveEmptyProducesMetadata)
                {
                    metadata = new EndpointMetadataCollection(new ProducesMetadata(Array.Empty<string>()));
                }
                else if (_contentTypes.Count > 0)
                {
                    metadata = new EndpointMetadataCollection(new ProducesMetadata(_contentTypes.ToArray()));
                }

                return new Endpoint(null, metadata, null);
            }

            public Endpoint WithEmptyProducesMetadata()
            {
                _shouldHaveEmptyProducesMetadata = true;
                return this;
            }

            public Endpoint WithContentType(string contentType)
            {
                _contentTypes.Clear();
                _contentTypes.Add(contentType);
                return this;
            }

            public Endpoint WithContentTypes(string contentType, params string[] contentTypes)
            {
                _contentTypes.Clear();
                _contentTypes.Add(contentType);
                _contentTypes.AddRange(contentTypes);
                return this;
            }
        }
    }
}
