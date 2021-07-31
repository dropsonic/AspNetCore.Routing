# ProducesMatcherPolicy

## Description

A special endpoint [`MatcherPolicy`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.matcherpolicy) that selects the endpoint based on the [`Accept`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept) header and [`ProducesAttribute`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.producesattribute) of the endpoints, thus allowing to have multiple matching endpoints but with different media types in [`ProducesAttribute`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.producesattribute).

## Remarks

The overall design follows the patterns from `Microsoft.AspNetCore.Mvc.Routing.ConsumesMatcherPolicy`, especially for the [`INodeBuilderPolicy`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.matching.inodebuilderpolicy) implementation.
