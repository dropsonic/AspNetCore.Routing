using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Dropsonic.AspNetCore.Routing
{
    public class ProducesMatcherOptions
    {
        public static class MediaTypes
        {
            public static class Text
            {
                public static readonly MediaType Html = new MediaType(MediaTypeNames.Text.Html);
            }

            public static class Application
            {
                public static readonly MediaType Json = new MediaType(AdditionalMediaTypeNames.Application.Json);
            }
        }

        /// <summary>
        /// The map of <c>$format=contentTypeName</c> query string values to media types that correspond them,
        /// in case if a user wants to specify the desired content type via query the <c>$format</c> query string parameter.
        /// </summary>
        /// <remarks>The keys (query string values) are case-insensitive.</remarks>
        public IDictionary<string, MediaType> UserDefinedFormatToContentTypeMappings { get; } =
            new Dictionary<string, MediaType>(StringComparer.OrdinalIgnoreCase)
            {
                { "html", MediaTypes.Text.Html },
                { "json", MediaTypes.Application.Json },
            };
    }
}
