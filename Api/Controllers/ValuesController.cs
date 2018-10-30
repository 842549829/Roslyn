using InterFace;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase, IOrder
    {
        [HttpGet("{id}")]
        public Order Add(int id)
        {
            return new Order();
        }

        [HttpPut]
        public Order Addx(string a)
        {
            throw new System.NotImplementedException();
        }

        [HttpPut]
        public Order Update([FromBody] Order value)
        {
            return value;
        }
    }
}