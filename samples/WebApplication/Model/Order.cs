using System.Runtime.Serialization;

namespace WebApplication.Model
{
    [DataContract]
    public class Order
    {
        [DataMember]
        public string Description { get; protected set; }

        [DataMember]
        public double Total { get; protected set; }

        public Order(string description, double total)
        {
            Description = description;
            Total = total;
        }
    }
}
