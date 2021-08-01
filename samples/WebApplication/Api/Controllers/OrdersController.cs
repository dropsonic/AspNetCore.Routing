using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Model;
using WebApplication.Services;

namespace WebApplication.Api.Controllers
{
    [Route(Routes.Orders)]
    [Produces(MediaTypeNames.Application.Json, MediaTypeNames.Application.Xml)]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;

        public OrdersController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Order>> GetAll() => Ok(_orderRepository.GetAll());
    }
}
