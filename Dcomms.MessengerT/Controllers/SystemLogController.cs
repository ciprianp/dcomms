﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Dcomms.MessengerT.Controllers
{
    public class SystemLogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}