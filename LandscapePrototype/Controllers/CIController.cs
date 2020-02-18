using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LandscapePrototype.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CIController : ControllerBase
    {
        private readonly ILogger<CIController> _logger;
        private readonly CIModel _ciModel;
        private readonly NpgsqlConnection _conn;

        public CIController(ILogger<CIController> logger, CIModel ciModel, NpgsqlConnection conn)
        {
            _logger = logger;
            _ciModel = ciModel;
            _conn = conn;
        }

        private string dbName = "landscape_prototype";

        [HttpGet]
        public async Task<IEnumerable<CIAttribute>> Get()
        {
            var attributes = await _ciModel.GetMergedAttributes("H123", true, new LayerSet(new long[] { }), null);
            return attributes;
        }
    }
}
