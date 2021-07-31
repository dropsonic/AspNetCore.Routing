using System;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dropsonic.AspNetCore.Routing
{
    public static class MvcBuilderExtensions
    {
        /// <summary>
        /// Adds a special endpoint <see cref="Microsoft.AspNetCore.Routing.MatcherPolicy"/> that selects the endpoint
        /// based on the <c>Accept</c> header and <see cref="Microsoft.AspNetCore.Mvc.ProducesAttribute"/> of the endpoints,
        /// thus allowing to have multiple matching endpoints but with different media types in <see cref="Microsoft.AspNetCore.Mvc.ProducesAttribute"/>.
        /// </summary>
        internal static IMvcBuilder AddProducesEndpointMatcher(this IMvcBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var services = builder.Services;
            
            services.AddOptions<ProducesMatcherOptions>();

            builder.AddMvcOptions(options =>
            {
                options.Conventions.Add(new ProducesConvention());
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, ProducesMatcherPolicy>());

            return builder;
        }

        /// <inheritdoc cref="AddProducesEndpointMatcher(Microsoft.Extensions.DependencyInjection.IMvcBuilder)"/>
        internal static IMvcBuilder AddProducesEndpointMatcher(
            this IMvcBuilder builder,
            Action<ProducesMatcherOptions> setupAction)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));
            
            builder.AddProducesEndpointMatcher();
            builder.Services.Configure(setupAction);

            return builder;
        }
    }
}
