using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;

namespace Dropsonic.AspNetCore.Routing
{
    /// <summary>
    /// A special endpoint <see cref="Microsoft.AspNetCore.Routing.MatcherPolicy"/> that selects the endpoint
    /// based on the <c>Accept</c> header and <see cref="Microsoft.AspNetCore.Mvc.ProducesAttribute"/> of the endpoints,
    /// thus allowing to have multiple matching endpoints but with different media types in <see cref="Microsoft.AspNetCore.Mvc.ProducesAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The overall design follows the patterns from <see cref="Microsoft.AspNetCore.Mvc.Routing.ConsumesMatcherPolicy"/>,
    /// especially for the <see cref="INodeBuilderPolicy"/> implementation.
    /// </remarks>>
    // ReSharper disable once UnusedMember.Global
    internal class ProducesMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, INodeBuilderPolicy
    {
        /// <remarks>
        /// <para>If <see cref="MvcOptions.RespectBrowserAcceptHeader"/> has a default value of <see langword="false" />, ASP.NET Core returns JSON, as stated in
        /// <see href="https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-5.0#browsers-and-content-negotiation">the documentation</see>.</para>
        /// <para>But in the case when multiple endpoints share the same route (i.e., MVC and web API), it makes sense to default to <c>text/html</c> for user's convenience first,
        /// and only if there is no suitable endpoint for that, return <c>application/json</c>.</para>
        /// </remarks>
        private static readonly IReadOnlyList<MediaType> DefaultContentTypes = new []
        {
            ProducesMatcherOptions.MediaTypes.Text.Html,
            ProducesMatcherOptions.MediaTypes.Application.Json
        };
        
        private const string UserDefinedContentTypeParameter = "$format";
        private const string Http406EndpointDisplayName = "HTTP 406 Not Acceptable";
        private const string AnyContentType = "*/*";

        private readonly IDictionary<string, MediaType> _userDefinedFormatToContentTypeMappings;
        private readonly bool _respectBrowserAcceptHeader;
        private readonly bool _returnHttpNotAcceptable;

        public ProducesMatcherPolicy(
            IOptions<ProducesMatcherOptions> options,
            IOptions<MvcOptions> mvcOptions)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (mvcOptions == null) throw new ArgumentNullException(nameof(mvcOptions));

            _userDefinedFormatToContentTypeMappings = options.Value.UserDefinedFormatToContentTypeMappings;
            _respectBrowserAcceptHeader = mvcOptions.Value.RespectBrowserAcceptHeader;
            _returnHttpNotAcceptable = mvcOptions.Value.ReturnHttpNotAcceptable;
        }

        /// <summary>
        /// Runs after <see cref="Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy"></see>
        /// and <see cref="Microsoft.AspNetCore.Mvc.Routing.ConsumesMatcherPolicy"/>
        /// but before <see cref="Microsoft.AspNetCore.Mvc.Routing.ActionConstraintMatcherPolicy"/>.
        /// </summary>
        public override int Order => 1;

        public IComparer<Endpoint> Comparer { get; } = new ProducesMetadataEndpointComparer();


        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

            return endpoints.Any(e => e.Metadata.GetMetadata<IProducesMetadata>()?.ContentTypes.Count > 0);
        }

        public IReadOnlyList<PolicyNodeEdge> GetEdges(IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null) throw new ArgumentNullException(nameof(endpoints));

            // The algorithm here is designed to be preserve the order of the endpoints
            // while also being relatively simple. Preserving order is important.

            // First, build a dictionary of all of the content-type patterns that are included
            // at this node.
            //
            // For now we're just building up the set of keys. We don't add any endpoints
            // to lists now because we don't want ordering problems.
            var edges = new Dictionary<string, List<Endpoint>>(StringComparer.OrdinalIgnoreCase);
            foreach (var endpoint in endpoints)
            {
                var contentTypes = endpoint.Metadata.GetMetadata<IProducesMetadata>()?.ContentTypes;
                if (contentTypes == null || contentTypes.Count == 0)
                {
                    contentTypes = new[] { AnyContentType };
                }

                foreach (var contentType in contentTypes)
                {
                    if (!edges.ContainsKey(contentType))
                    {
                        edges.Add(contentType, new List<Endpoint>());
                    }
                }
            }

            // Now in a second loop, add endpoints to these lists. We've enumerated all of
            // the states, so we want to see which states this endpoint matches.
            foreach (var endpoint in endpoints)
            {
                var contentTypes = endpoint.Metadata.GetMetadata<IProducesMetadata>()?.ContentTypes ?? Array.Empty<string>();
                if (contentTypes.Count == 0)
                {
                    // OK this means that this endpoint matches *all* content methods.
                    // So, loop and add it to all states.
                    foreach (var kvp in edges)
                    {
                        kvp.Value.Add(endpoint);
                    }
                }
                else
                {
                    // OK this endpoint matches specific content types -- we have to loop through edges here
                    // because content types could either be exact (like 'application/json') or they
                    // could have wildcards (like 'text/*'). We don't expect wildcards to be especially common
                    // with consumes, but we need to support it.
                    foreach (var kvp in edges)
                    {
                        // The edgeKey maps to a possible request header value
                        var edgeKey = new MediaType(kvp.Key);

                        foreach (var contentType in contentTypes)
                        {
                            var mediaType = new MediaType(contentType);

                            // Example: 'application/json' is subset of 'application/*'
                            // 
                            // This means that when the request has content-type 'application/json' an endpoint
                            // what consumes 'application/*' should match.
                            if (edgeKey.IsSubsetOf(mediaType))
                            {
                                kvp.Value.Add(endpoint);

                                // It's possible that a ConsumesMetadata defines overlapping wildcards. Don't add an endpoint
                                // to any edge twice
                                break;
                            }
                        }
                    }
                }
            }

            // If after we're done there isn't any endpoint that accepts */*, then we'll synthesize an
            // endpoint that always returns a 415.
            if (!edges.ContainsKey(AnyContentType))
            {
                edges.Add(AnyContentType, new List<Endpoint>()
                {
                    CreateRejectionEndpoint(),
                });
            }

            return edges
                .Select(kvp => new PolicyNodeEdge(kvp.Key, kvp.Value))
                .ToArray();
        }

        private Endpoint CreateRejectionEndpoint()
        {
            return new Endpoint(
                (context) =>
                {
                    context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                    return Task.CompletedTask;
                },
                EndpointMetadataCollection.Empty,
                Http406EndpointDisplayName);
        }

        public PolicyJumpTable BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));

            // Since our 'edges' can have wildcards, we do a sort based on how wildcard-ey they
            // are then then execute them in linear order.
            (MediaType MediaType, int Destination)[] ordered = edges
                .Select(e => (MediaType: new MediaType((string) e.State), e.Destination))
                .OrderBy(e => GetScore(e.MediaType))
                .ToArray();

            // If any edge matches all content types, then treat that as the 'exit'. This will
            // always happen because we insert a 406 endpoint.
            for (var i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].MediaType.MatchesAllTypes)
                {
                    exitDestination = ordered[i].Destination;
                    break;
                }
            }

            return new ProducesPolicyJumpTable(exitDestination, ordered, _userDefinedFormatToContentTypeMappings,
                _respectBrowserAcceptHeader, _returnHttpNotAcceptable);
        }

        private int GetScore(in MediaType mediaType)
        {
            // Higher score == lower priority - see comments on MediaType.
            if (mediaType.MatchesAllTypes)
                return 4;

            if (mediaType.MatchesAllSubTypes)
                return 3;

            if (mediaType.MatchesAllSubTypesWithoutSuffix)
                return 2;

            return 1;
        }

        private class ProducesMetadataEndpointComparer : EndpointMetadataComparer<IProducesMetadata>
        {
            protected override int CompareMetadata(IProducesMetadata x, IProducesMetadata y)
            {
                // Ignore the metadata if it has an empty list of content types.
                return base.CompareMetadata(
                    x?.ContentTypes.Count > 0 ? x : null,
                    y?.ContentTypes.Count > 0 ? y : null);
            }
        }

        private class ProducesPolicyJumpTable : PolicyJumpTable
        {
            private readonly int _exitDestination;
            private readonly (MediaType MediaType, int Destination)[] _destinations;
            private readonly IDictionary<string, MediaType> _userDefinedFormatToContentTypeMappings;
            private readonly bool _respectBrowserAcceptHeader;
            private readonly bool _returnHttpNotAcceptable;

            public ProducesPolicyJumpTable(int exitDestination, (MediaType MediaType, int Destination)[] destinations,
                IDictionary<string, MediaType> userDefinedFormatToContentTypeMappings,
                bool respectBrowserAcceptHeader, bool returnHttpNotAcceptable)
            {
                _exitDestination = exitDestination;
                _destinations = destinations;
                _userDefinedFormatToContentTypeMappings = userDefinedFormatToContentTypeMappings;
                _respectBrowserAcceptHeader = respectBrowserAcceptHeader;
                _returnHttpNotAcceptable = returnHttpNotAcceptable;
            }

            private IReadOnlyList<MediaType> GetAcceptableMediaTypesSortedByQuality(HttpRequest request)
            {
                var result = new List<MediaType>();

                // $format=XXX in the query string has the highest priority
                if (request.Query.TryGetValue(UserDefinedContentTypeParameter, out var userDefinedFormatNames))
                {
                    foreach (var userDefinedFormatName in userDefinedFormatNames
                        .Where(n => !String.IsNullOrWhiteSpace(n)))
                    {
                        if (_userDefinedFormatToContentTypeMappings.TryGetValue(userDefinedFormatName, out var mediaType))
                        {
                            result.Add(mediaType);
                        }
                    }

                    return result;
                }
                
                foreach (var mediaTypeWithQuality in request.GetTypedHeaders().Accept
                    .OrderByDescending(mt => mt.Quality ?? 1))
                {
                    var mediaType = new MediaType(mediaTypeWithQuality.MediaType);

                    if (!_respectBrowserAcceptHeader && mediaType.MatchesAllSubTypes && mediaType.MatchesAllTypes)
                    {
                        return DefaultContentTypes;
                    }

                    result.Add(mediaType);
                }

                if (result.Count == 0)
                {
                    return DefaultContentTypes;
                }

                return result;
            }

            public override int GetDestination(HttpContext httpContext)
            {
                if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
                
                // If we have a single matching endpoint, no need to select anything
                if (_destinations.Length == 1)
                    return _destinations[0].Destination;

                var mediaTypes = GetAcceptableMediaTypesSortedByQuality(httpContext.Request);

                foreach (var mediaType in mediaTypes)
                {
                    int? matchedDestination = null;

                    foreach (var destination in _destinations)
                    {
                        if (destination.MediaType.IsSubsetOf(mediaType))
                        {
                            if (matchedDestination != null)
                            {
                                throw new AmbiguousMatchException(
                                    $"The request matched multiple endpoints for the media type {mediaType.Type}/{mediaType.SubType}.");
                            }

                            matchedDestination = destination.Destination;
                        }
                    }

                    if (matchedDestination.HasValue)
                        return matchedDestination.Value;
                }

                return _returnHttpNotAcceptable ? _exitDestination : _destinations[0].Destination;
            }
        }
    }
}
