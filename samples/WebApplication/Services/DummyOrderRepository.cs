using System.Collections.Generic;
using WebApplication.Model;

namespace WebApplication.Services
{
    internal class DummyOrderRepository : IOrderRepository
    {
        public IEnumerable<Order> GetAll()
        {
            yield return new Order("Coffee", 200);
            yield return new Order("Cookies", 70);
            yield return new Order("IT Services", 100500);
        }
    }
}
