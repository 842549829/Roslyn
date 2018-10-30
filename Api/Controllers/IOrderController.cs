using InterFace;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IOrderController : ControllerBase, IOrder
    {
        [HttpGet("{id}")]
        public Order Add(int id)
        {
            return new Order();
        }

        [HttpGet("addorder/{id}/{name}")]
        public Order AddOrder(int id, string name)
        {
            return new Order { Id = id, Name = name };
        }

        [HttpPut]
        public Order Addx(string a)
        {
            throw new System.NotImplementedException();
        }

        [HttpPost]
        public Order Update([FromBody] Order value)
        {
            return value;
        }
    }
}