using System.Collections.Generic;

namespace Dropsonic.AspNetCore.Routing
{
    internal interface IProducesMetadata
    {
        IReadOnlyList<string> ContentTypes { get; }
    }
}
