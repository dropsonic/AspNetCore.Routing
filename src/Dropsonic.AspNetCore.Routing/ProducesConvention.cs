using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Dropsonic.AspNetCore.Routing
{
    internal class ProducesConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            var producesAttribute = action.Attributes.OfType<ProducesAttribute>().FirstOrDefault() ??
                                    action.Controller.Attributes.OfType<ProducesAttribute>().FirstOrDefault();
            
            if (producesAttribute != null)
            {
                foreach (var selector in action.Selectors)
                    selector.EndpointMetadata.Add(new ProducesMetadata(producesAttribute.ContentTypes.ToArray()));
            }
        }
    }
}
