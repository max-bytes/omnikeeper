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
        private readonly CIModel _ciModel;

        public CIController(ILogger<CIController> logger, CIModel ciModel)
        {
            _logger = logger;
            _ciModel = ciModel;
        }

        private string dbName = "landscape_prototype";

        [HttpGet]
        public IEnumerable<CIAttribute> Get()
        {
            var attributes = _ciModel.GetMergedAttributes("H123", true, new LayerSet(new long[] { }));
            return attributes;
        }
    }
}
