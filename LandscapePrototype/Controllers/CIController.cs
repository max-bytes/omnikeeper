using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LandscapePrototype.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CIController : ControllerBase
    {
        private readonly ILogger<CIController> _logger;

        public CIController(ILogger<CIController> logger)
        {
            _logger = logger;
        }

        private string dbName = "landscape_prototype";

        [HttpGet]
        public IEnumerable<CIAttribute> Get()
        {
            var model = new CIModel();

            using (var conn = model.CreateOpenConnection(dbName))
            {
                var attributes = model.GetMergedAttributes("H123", true, conn);
                return attributes;
            }
        }
    }
}
