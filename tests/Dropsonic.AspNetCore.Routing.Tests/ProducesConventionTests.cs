using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local

namespace Dropsonic.AspNetCore.Routing.Tests
{
    public class ProducesConventionTests
    {
        public class ProducesAttributeOnControllerOnly
        {
            [Route("test")]
            [Produces("application/json")]
            private class TestController : ControllerBase
            {
                [HttpGet]
                public IActionResult Get() => NoContent();
            }

            [Fact]
            public void ShouldReturnProducesFromController()
            {
                // Arrange
                IActionModelConvention convention = new ProducesConvention();
                var action = CreateActionModel<TestController>(nameof(TestController.Get));
            
                // Act
                convention.Apply(action);

                // Assert
                action.Selectors.Should().HaveCount(1); // sanity check
                var metadata = action.Selectors[0].EndpointMetadata.OfType<IProducesMetadata>().FirstOrDefault();
                metadata.Should().NotBeNull().And.BeEquivalentTo(new ProducesMetadata(new [] { "application/json" }));
            }
        }

        public class ProducesAttributeOnActionOnly
        {
            [Route("test")]
            private class TestController : ControllerBase
            {
                [HttpGet]
                [Produces("application/xml")]
                public IActionResult Get() => NoContent();
            }

            [Fact]
            public void ShouldReturnProducesFromAction()
            {
                // Arrange
                IActionModelConvention convention = new ProducesConvention();
                var action = CreateActionModel<TestController>(nameof(TestController.Get));
            
                // Act
                convention.Apply(action);

                // Assert
                action.Selectors.Should().HaveCount(1); // sanity check
                var metadata = action.Selectors[0].EndpointMetadata.OfType<IProducesMetadata>().FirstOrDefault();
                metadata.Should().NotBeNull().And.BeEquivalentTo(new ProducesMetadata(new [] { "application/xml" }));
            }
        }

        public class ProducesAttributeBothOnControllerAndAction
        {
            [Route("test")]
            [Produces("application/json")]
            private class TestController : ControllerBase
            {
                [HttpGet]
                [Produces("application/xml")]
                public IActionResult Get() => NoContent();
            }

            [Fact]
            public void ShouldReturnProducesFromAction()
            {
                // Arrange
                IActionModelConvention convention = new ProducesConvention();
                var action = CreateActionModel<TestController>(nameof(TestController.Get));
            
                // Act
                convention.Apply(action);

                // Assert
                action.Selectors.Should().HaveCount(1); // sanity check
                var metadata = action.Selectors[0].EndpointMetadata.OfType<IProducesMetadata>().FirstOrDefault();
                metadata.Should().NotBeNull().And.BeEquivalentTo(new ProducesMetadata(new [] { "application/xml" }), 
                    "because ProducesAttribute on an action has a higher priority than on a controller");
            }
        }

        public class ProducesAttributeWithMultipleContentTypesOnController
        {
            [Route("test")]
            [Produces("application/json", "text/html", "application/xml")]
            private class TestController : ControllerBase
            {
                [HttpGet]
                public IActionResult Get() => NoContent();
            }

            [Fact]
            public void ShouldReturnProducesFromController()
            {
                // Arrange
                IActionModelConvention convention = new ProducesConvention();
                var action = CreateActionModel<TestController>(nameof(TestController.Get));
            
                // Act
                convention.Apply(action);

                // Assert
                action.Selectors.Should().HaveCount(1); // sanity check
                var metadata = action.Selectors[0].EndpointMetadata.OfType<IProducesMetadata>().FirstOrDefault();
                metadata.Should().NotBeNull().And.BeEquivalentTo(new ProducesMetadata(new [] { "application/json", "text/html", "application/xml" }));
            }
        }

        public class NoProducesAttribute
        {
            [Route("test")]
            private class TestController : ControllerBase
            {
                [HttpGet]
                public IActionResult Get() => NoContent();
            }

            [Fact]
            public void ShouldNotHaveProducesMetadata()
            {
                // Arrange
                IActionModelConvention convention = new ProducesConvention();
                var action = CreateActionModel<TestController>(nameof(TestController.Get));
            
                // Act
                convention.Apply(action);

                // Assert
                action.Selectors.Should().HaveCount(1); // sanity check
                action.Selectors[0].EndpointMetadata.OfType<IProducesMetadata>().Should().BeEmpty();
            }
        }

        private static ActionModel CreateActionModel<TController>(string actionName)
            where TController : ControllerBase
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(); // required by Mvc
            services.AddMvc();
            var serviceProvider = services.BuildServiceProvider();

            var applicationModelProviders = serviceProvider.GetServices<IApplicationModelProvider>();
            var applicationModelProviderContext = new ApplicationModelProviderContext(new [] { typeof(TController).GetTypeInfo() });

            foreach (var applicationModelProvider in applicationModelProviders)
            {
                applicationModelProvider.OnProvidersExecuting(applicationModelProviderContext);
                applicationModelProvider.OnProvidersExecuted(applicationModelProviderContext);
            }

            return applicationModelProviderContext.Result.Controllers[0].Actions
                .First(action => action.ActionName == actionName);
        }
    }
}
