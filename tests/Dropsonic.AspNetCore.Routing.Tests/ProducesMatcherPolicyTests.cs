using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Routing.Matching;
using Xunit;

namespace Dropsonic.AspNetCore.Routing.Tests
{
        public partial class ProducesMatcherPolicyTests
    {
        [Fact]
        public void ShouldRanAfterBuiltInPolicies()
        {
            // Arrange
            var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

            // Act/Assert
            policy.Order.Should().BePositive(
                "because ProducesMatcherPolicy should be applied before built-in policies from the same category (they have negative order)");
        }

        [Fact]
        public void ShouldPreferEndpointsWithCorrespondingMetadata()
        {
            // Arrange
            var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

            var endpointWithoutMetadata = Make.Endpoint();
            var endpointWithCorrespondingMetadata = Make.Endpoint().WithContentType("application/json");
            var endpointWithEmptyCorrespondingMetadata = Make.Endpoint().WithEmptyProducesMetadata();

            // Act/Assert
            policy.Comparer.Compare(endpointWithoutMetadata, endpointWithCorrespondingMetadata)
                .Should().BePositive();
            policy.Comparer.Compare(endpointWithoutMetadata, endpointWithEmptyCorrespondingMetadata)
                .Should().Be(0);
            policy.Comparer.Compare(endpointWithCorrespondingMetadata, endpointWithEmptyCorrespondingMetadata)
                .Should().BeNegative();
        }

        [Fact]
        public void NoContentTypeOnEndpoint_PolicyShouldNotBeApplicable()
        {
            // Arrange
            var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());
            var endpoints = new []
            {
                Make.Endpoint(),
                Make.Endpoint().WithEmptyProducesMetadata()
            };
            
            // Act/Assert
            policy.AppliesToEndpoints(endpoints).Should().BeFalse("because there are no endpoints with associated content types");
        }

        [Fact]
        public void MatchAllSubtypeAndQualityInAccept_ShouldSelectSpecificEndpoint()
        {
            // Arrange
            var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

            var endpoints = new[]
            {
                Make.Endpoint().WithContentType("application/xml"),
                Make.Endpoint().WithContentType("application/json"),
                Make.Endpoint().WithContentTypes("image/png"),
                Make.Endpoint().WithContentType("text/html"),
            };

            // Act
            bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
            var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
            var jumpTable = policy.BuildJumpTable(-1, edges);
            int actualDestination = jumpTable.GetDestination(
                Make.HttpContext().WithAcceptHeader("text/plain, image/*;q=0.9, application/xml;q=0.8"));
            string actualContentType = (string) edges[actualDestination].State;

            // Assert
            appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
            actualContentType.Should().Be("image/png", "because it is the matching content type");
        }

        public class NonMatchingContentTypes
        {
            [Fact]
            public void ShouldReturnFirstEndpoint()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentType("application/xml"),
                    Make.Endpoint().WithContentTypes("text/html", "image/*"),
                };

                var httpContext = Make.HttpContext().WithAcceptHeader("application/json");

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(httpContext);
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualContentType.Should().Be("application/xml", 
                    "because the first endpoint should be selected by default, even if its content type doesn't match");
            }

            [Fact]
            public void WithReturnHttpNotAcceptableInMvcOptions_ShouldReturn406NotAcceptableEndpoint()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(),
                    Make.MvcOptions(options => { options.ReturnHttpNotAcceptable = true; }));

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentType("application/xml"),
                    Make.Endpoint().WithContentTypes("text/html", "image/*"),
                };

                var httpContext = Make.HttpContext().WithAcceptHeader("application/json");

                const int httpNotAcceptableDestination = 3; // zero-based content types count plus one

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(httpContext);
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualDestination.Should().Be(httpNotAcceptableDestination, "because it is the destination of the 406 HTTP Not Acceptable endpoint");
                actualContentType.Should().Be("*/*", "because it is the content type of the 406 HTTP Not Acceptable endpoint");
            }
        }

        public class TextHtmlEndpointExists
        {
            [Fact]
            public void ShouldPreferTextHtmlByDefault()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentType("application/json"),
                    Make.Endpoint().WithContentTypes("text/html"),
                };

                var httpContext = Make.HttpContext().WithAcceptHeader("*/*");

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(httpContext);
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualContentType.Should().Be("text/html",
                    "because text/html is the default content type, and a corresponding endpoint exists");
            }

            public class NoAcceptHeader
            {
                [Fact]
                public void ShouldPreferTextHtmlByDefault()
                {
                    // Arrange
                    var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                    var endpoints = new[]
                    {
                        Make.Endpoint().WithContentType("application/json"),
                        Make.Endpoint().WithContentTypes("text/html"),
                    };

                    var httpContext = Make.HttpContext();

                    // Act
                    bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                    var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                    var jumpTable = policy.BuildJumpTable(-1, edges);
                    int actualDestination = jumpTable.GetDestination(httpContext);
                    string actualContentType = (string) edges[actualDestination].State;

                    // Assert
                    appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                    actualContentType.Should().Be("text/html",
                        "because text/html is the default content type, and a corresponding endpoint exists");
                }
            }

            public class SpecificContentTypeAndMatchAllInAccept
            {
                [Fact]
                public void ShouldPreferTextHtmlByDefault()
                {
                    // Arrange
                    var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                    var endpoints = new[]
                    {
                        Make.Endpoint().WithContentType("application/json"),
                        Make.Endpoint().WithContentType("text/html"),
                    };

                    // Act
                    bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                    var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                    var jumpTable = policy.BuildJumpTable(-1, edges);
                    int actualDestination = jumpTable.GetDestination(
                        Make.HttpContext().WithAcceptHeader("application/json, */*;q=0.9"));
                    string actualContentType = (string) edges[actualDestination].State;

                    // Assert
                    appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                    actualContentType.Should().Be("text/html",
                        "because if Accept contains match-all content-type, even with lower quality, text/html must be preferred");
                }

                [Fact]
                public void WithRespectBrowserAcceptHeaderInMvcOptions_ShouldRespectBrowserAcceptHeader()
                {
                    // Arrange
                    var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(),
                        Make.MvcOptions(options => { options.RespectBrowserAcceptHeader = true; }));

                    var endpoints = new[]
                    {
                        Make.Endpoint().WithContentType("application/json"),
                        Make.Endpoint().WithContentType("text/html"),
                    };

                    // Act
                    bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                    var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                    var jumpTable = policy.BuildJumpTable(-1, edges);
                    int actualDestination = jumpTable.GetDestination(
                        Make.HttpContext().WithAcceptHeader("application/json, */*;q=0.9"));
                    string actualContentType = (string) edges[actualDestination].State;

                    // Assert
                    appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                    actualContentType.Should().Be("application/json",
                        "because if MvcOptions.RespectBrowserAcceptHeader is set to true, " +
                        "and there is a matching endpoint for the specific content type in Accept, it must be selected");
                }
            }
        }

        public class NoTextHtmlEndpoint
        {
            [Fact]
            public void ShouldPreferApplicationJsonByDefault()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentType("application/xml"),
                    Make.Endpoint().WithContentTypes("application/json"),
                };

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(Make.HttpContext().WithAcceptHeader("*/*"));
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualContentType.Should().Be("application/json",
                    "because application/json is the second default content type in case if there is no text/html endpoint");
            }

            public class SpecificContentTypeAndMatchAllInAccept
            {
                [Fact]
                public void ShouldPreferApplicationJsonAsSecondDefault()
                {
                    // Arrange
                    var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                    var endpoints = new[]
                    {
                        Make.Endpoint().WithContentType("text/plain"),
                        Make.Endpoint().WithContentType("application/json"),
                    };

                    // Act
                    bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                    var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                    var jumpTable = policy.BuildJumpTable(-1, edges);
                    int actualDestination = jumpTable.GetDestination(
                        Make.HttpContext().WithAcceptHeader("text/plain, */*;q=0.9"));
                    string actualContentType = (string) edges[actualDestination].State;

                    // Assert
                    appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                    actualContentType.Should().Be("application/json",
                        "because if Accept contains match-all content-type, even with lower quality, and there is no endpoint for text/html, " +
                        "application/json must be preferred as the second default option");
                }

                [Fact]
                public void WithRespectBrowserAcceptHeaderInMvcOptions_ShouldRespectBrowserAcceptHeader()
                {
                    // Arrange
                    var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(),
                        Make.MvcOptions(options => { options.RespectBrowserAcceptHeader = true; }));

                    var endpoints = new[]
                    {
                        Make.Endpoint().WithContentType("text/plain"),
                        Make.Endpoint().WithContentType("application/json"),
                    };

                    // Act
                    bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                    var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                    var jumpTable = policy.BuildJumpTable(-1, edges);
                    int actualDestination = jumpTable.GetDestination(
                        Make.HttpContext().WithAcceptHeader("text/plain, */*;q=0.9"));
                    string actualContentType = (string) edges[actualDestination].State;

                    // Assert
                    appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                    actualContentType.Should().Be("text/plain",
                        "because if MvcOptions.RespectBrowserAcceptHeader is set to true, " +
                        "and there is a matching endpoint for the specific content type in Accept, it must be selected");
                }
            }
        }

        public class CustomFormatInQueryString
        {
            [Fact]
            public void ShouldReturnContentTypeRequestedByTheFormatParameter()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.DefaultProducesMatcherOptions(), Make.DefaultMvcOptions());

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentTypes("text/html"),
                    Make.Endpoint().WithContentTypes("application/json"),
                };

                var httpContext = Make.HttpContext()
                    .WithAcceptHeader("text/html")
                    .WithQueryStringParameter("$format", "json");

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(httpContext);
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualContentType.Should().Be("application/json", 
                    "because it is the content type explicitly requested by the $format query string parameter");
            }

            [Fact]
            public void CustomContentTypeInProducesMatchedOptions_ShouldReturnContentTypeRequestedByTheFormatParameter()
            {
                // Arrange
                var policy = new ProducesMatcherPolicy(Make.ProducesMatcherOptions(options =>
                {
                    options.UserDefinedFormatToContentTypeMappings["xml"] = new MediaType("application/xml");
                }), Make.DefaultMvcOptions());

                var endpoints = new[]
                {
                    Make.Endpoint().WithContentTypes("text/html"),
                    Make.Endpoint().WithContentTypes("application/json"),
                    Make.Endpoint().WithContentTypes("application/xml"),
                };

                var httpContext = Make.HttpContext()
                    .WithAcceptHeader("text/html")
                    .WithQueryStringParameter("$format", "xml");

                // Act
                bool appliesToEndpoints = policy.AppliesToEndpoints(endpoints);
                var edges = policy.GetEdges(endpoints).ToJumpTableEdges();
                var jumpTable = policy.BuildJumpTable(-1, edges);
                int actualDestination = jumpTable.GetDestination(httpContext);
                string actualContentType = (string) edges[actualDestination].State;

                // Assert
                appliesToEndpoints.Should().BeTrue("because there are endpoints with associated content types");
                actualContentType.Should().Be("application/xml", 
                    "because it is the content type explicitly requested by the $format query string parameter");
            }
        }
    }


    internal static class PolicyNodeEdgeEnumerableExtensions
    {
        public static PolicyJumpTableEdge[] ToJumpTableEdges(this IEnumerable<PolicyNodeEdge> edges) =>
            edges.Select((edge, i) => new PolicyJumpTableEdge(edge.State, i)).ToArray();
    }
}
