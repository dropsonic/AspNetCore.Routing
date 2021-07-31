using System;
using System.Collections.Generic;

namespace Dropsonic.AspNetCore.Routing
{
    internal class ProducesMetadata : IProducesMetadata
    {
        public ProducesMetadata(string[] contentTypes)
        {
            ContentTypes = contentTypes ?? throw new ArgumentNullException(nameof(contentTypes));
        }
        
        public IReadOnlyList<string> ContentTypes { get; }
    }
}
