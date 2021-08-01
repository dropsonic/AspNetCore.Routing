# Dropsonic.AspNetCore.Routing

[![CI](https://github.com/dropsonic/AspNetCore.Routing/actions/workflows/ci.yml/badge.svg)](https://github.com/dropsonic/AspNetCore.Routing/actions/workflows/ci.yml)

## ProducesMatcherPolicy

### Overview

A special endpoint [`MatcherPolicy`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.matcherpolicy) that selects the endpoint based on the [`Accept`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept) header (or the explicit content type passed as a query string parameter) and [`ProducesAttribute`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.producesattribute) of the endpoints, thus allowing to have multiple matching endpoints but with different media types in [`ProducesAttribute`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.producesattribute).

### Scenarios

Consider having a resource that you want to serve, providing a RESTful endpoint to do that.

Sometimes it is very convenient to consider HTML as another representation of this resource, alongside with JSON or XML served by a web API. The original architectural pattern (REST) described in [a dissertation by Roy Fielding](https://www.ics.uci.edu/~fielding/pubs/dissertation/fielding_dissertation.pdf) doesn't mention any specific represenatation formats so serving both HTML and JSON from the same RESTful endpoint is inline with that.

In a real-life scenario, we typically have a single-page application (SPA) that is served from any valid route. The SPA has its own client-side routing, and queries the web API, processing the retrieved data in JSON to display the actual HTML markup.

The suggested approach, when used in conjunction with [HATEOAS](https://en.wikipedia.org/wiki/HATEOAS), provides some major benefits:

- The SPA doesn't need to know anything about URIs at all (no hardcoded URIs on the client). The SPA is served
- URIs can have the same format both in API and in SPA routing.

Unfortunately, putting both MVC controller and API controller on the same route in ASP.NET Core produces an `AmbiguousMatchException` exception since the framework cannot choose an appropriate endpoint. `ProducesMatcherPolicy` helps with that, choosing the endpoint based on the [`Accept`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept) header, matching its value with the [`ProducesAttribute`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.producesattribute) on endpoints.

### Usage

```c#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc()
            .AddProducesEndpointMatcher(); // (!)
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
```

```c#
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

[Route("orders")] // the same route as in the API controller
[Produces(MediaTypeNames.Text.Html)]
public class OrdersController : Controller
{
    [HttpGet("{**catchAll}")] // SPA fallback for client-side routing; an on-site equivalent of MapSpaFallbackRoute()
    public IActionResult Index(string catchAll) => View();
}
```

```c#
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

[Route("orders")] // the same route as in the MVC controller
[Produces(MediaTypeNames.Application.Json)]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetOrders()
    {
        ...
    }
}
```

You can also find a sample web application in

### Behavior

If [`MvcOptions.RespectBrowserAcceptHeader`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.mvcoptions.respectbrowseracceptheader) has a default value of `false`, ASP.NET Core returns JSON, [as stated in the documentation](https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting#browsers-and-content-negotiation). But in the case when multiple endpoints share the same route (i.e., MVC and web API), it makes sense to default to `text/html` for user's convenience first, and only if there is no suitable endpoint for that, return `application/json`.

```
GET /orders
Accept: */*

-- HTML is served, if possible; JSON is a fallback option
```

For debug purposes, it is very typical to query the web API directly from the browser and/or 3rd-party tools such as [Postman](https://www.postman.com), omitting the [`Accept`](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Accept) header. To support this scenario, you can pass the `$format` query parameter (similar to [OData v3](https://www.odata.org/documentation/odata-version-3-0/url-conventions/#url5.1.8) and [OData v4](https://docs.oasis-open.org/odata/odata-json-format/v4.01/odata-json-format-v4.01.html#sec_RequestingtheJSONFormat)), providing an explicit content type abbreviation:

```
GET /orders?$format=json
Accept: */*

-- JSON is served
```

The set of abbreviations and corresponding content types can be customized in the options:

```c#
builder
    .AddMvc()
    .AddProducesEndpointMatcher(options =>
    {
        options.UserDefinedFormatToContentTypeMappings["xml"] = new MediaType("application/xml");
    });
```

```
GET /orders?$format=xml
Accept: */*

-- An endpoint that produces application/xml is called
```

Note that this option affects only the `ProducesMatcherPolicy` behavior and does not affect the default ASP.NET Core content negotiation process. It won't force ASP.NET Core to serve this particular content type, bypassing other content negotiation rules and settings such as [`MvcOptions.RespectBrowserAcceptHeader`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.mvcoptions.respectbrowseracceptheader).

## Remarks

The overall design follows the patterns from `Microsoft.AspNetCore.Mvc.Routing.ConsumesMatcherPolicy`, especially for the [`INodeBuilderPolicy`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.routing.matching.inodebuilderpolicy) implementation.

The unit tests use some techniques heavely inspired by the book [Working Effectively with Unit Tests by Jay Fields (Goodreads Author), Michael C. Feathers](https://www.goodreads.com/book/show/22605938-working-effectively-with-unit-tests).
