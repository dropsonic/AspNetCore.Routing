using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Xunit;
// ReSharper disable ClassNeverInstantiated.Local

namespace Dropsonic.AspNetCore.Routing.Tests
{
    public class ProducesConventionTests
    {
        public class ProducesAttributeOnControllerOnly
        {
            [ApiController]
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
            [ApiController]
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
            [ApiController]
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
            [ApiController]
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
            [ApiController]
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
            var mvcOptions = new OptionsWrapper<MvcOptions>(new MvcOptions());
            var compositeMetadataDetailsProvider = new DefaultCompositeMetadataDetailsProvider(Enumerable.Empty<IMetadataDetailsProvider>());
            var modelMetadataProvider = new DefaultModelMetadataProvider(compositeMetadataDetailsProvider);
            IApplicationModelProvider applicationModelProvider = new DefaultApplicationModelProvider(mvcOptions, modelMetadataProvider);
            var applicationModelProviderContext = new ApplicationModelProviderContext(new [] { typeof(TController).GetTypeInfo() });
            applicationModelProvider.OnProvidersExecuting(applicationModelProviderContext);
            applicationModelProvider.OnProvidersExecuted(applicationModelProviderContext);

            return applicationModelProviderContext.Result.Controllers[0].Actions
                .First(action => action.ActionName == actionName);
        }
    }
}
