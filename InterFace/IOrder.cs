using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Model;
using Refit;

namespace InterFace
{
    [Export, Bundle]
    public interface IOrder
    {
        [HttpGet]
        Order Add(int id);

        [HttpPost]
        Order Update(Order order);

        [HttpGet]
        Order Addx(string a);

    }

    [Export]
    public interface IService : IDisposable
    {
    }
}