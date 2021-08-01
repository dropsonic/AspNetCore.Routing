using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dropsonic.AspNetCore.Routing.Tests
{
    public class MvcBuilderExtensionsTests
    {
        [Fact]
        public void AddProducesEndpointMatcher_AddsProducesConvention_ToConventionsList()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcBuilder(services);

            // Act
            var actualBuilder = builder.AddProducesEndpointMatcher();

            // Assert
            actualBuilder.Should().NotBeNull().And.BeSameAs(builder);

            services.Where(x => x.ServiceType == typeof(MatcherPolicy))
                .Should()
                .ContainSingle(x => x.ImplementationType == typeof(ProducesMatcherPolicy));
        }

        [Fact]
        public void AddProducesEndpointMatcher_AddsProducesConvention_ToConventionsList_WithOptionsSetup()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new MvcBuilder(services);

            // Act
            var actualBuilder = builder.AddProducesEndpointMatcher(_ => { });

            // Assert
            actualBuilder.Should().NotBeNull().And.BeSameAs(builder);

            services.Where(x => x.ServiceType == typeof(MatcherPolicy))
                .Should()
                .ContainSingle(x => x.ImplementationType == typeof(ProducesMatcherPolicy));

            services
                .Should()
                .ContainSingle(x => x.ServiceType == typeof(IConfigureOptions<ProducesMatcherOptions>));
        }
    }
}
