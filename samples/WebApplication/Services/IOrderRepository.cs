using System.Collections.Generic;
using WebApplication.Model;

namespace WebApplication.Services
{
    public interface IOrderRepository
    {
        IEnumerable<Order> GetAll();
    }
}
