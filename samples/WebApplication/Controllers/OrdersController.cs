using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Services;

namespace WebApplication.Controllers
{
    [Route(Routes.Orders)]
    [Produces(MediaTypeNames.Text.Html)]
    public class OrdersController : Controller
    {
        private readonly IOrderRepository _orderRepository;

        public OrdersController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public IActionResult Index() => View(_orderRepository.GetAll());
    }
}
