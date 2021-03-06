﻿using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Model;
using Refit;

namespace InterFace
{
    [Export, Bundle]
    public interface IOrder : IService
    {
        [HttpGet]
        Order Add(int id);

        [HttpGet("addorder/{id}/{name}")]
        Order AddOrder(int id, string name);

        [HttpPost]
        Order Update(Order order);

        [HttpGet]
        Order Addx(string a);
    }

    [Export]
    public interface IService 
    {
    }
}