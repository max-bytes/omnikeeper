using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LandscapePrototype.Controllers
{
    [Controller]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class TestController : ControllerBase
    {
        private readonly ILogger<CIController> _logger;
        private readonly CIModel _ciModel;
        private readonly NpgsqlConnection _conn;

        public TestController(ILogger<CIController> logger, CIModel ciModel, NpgsqlConnection conn)
        {
            _logger = logger;
            _ciModel = ciModel;
            _conn = conn;
        }

        [HttpGet]
        public string Get()
        {
            return "authenticated";
        }
    }
}
