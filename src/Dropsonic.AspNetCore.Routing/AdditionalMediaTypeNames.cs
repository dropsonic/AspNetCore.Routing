namespace Dropsonic.AspNetCore.Routing
{
    /// <summary>
    /// Contains names of standard MIME types that do not exist in <see cref="System.Net.Mime.MediaTypeNames"/>.
    /// </summary>
    /// <remarks>Not named <c>MediaTypeNames</c> to avoid possible conflicts with <see cref="System.Net.Mime.MediaTypeNames"/>.</remarks>
    internal static class AdditionalMediaTypeNames
    {
        public static class Application
        {
            // ReSharper disable once InvalidXmlDocComment
            /// <remarks>
            /// Should be <see cref="System.Net.Mime.MediaTypeNames.Application.Json"/> but we don't have it
            /// in the current version of ASP.NET Core (see <see href="https://github.com/dotnet/runtime/issues/24597"/>).
            /// </remarks>
            /// <seealso href="https://www.ietf.org/rfc/rfc4627.txt"/>
            public const string Json = "application/json";
        }
    }
}
