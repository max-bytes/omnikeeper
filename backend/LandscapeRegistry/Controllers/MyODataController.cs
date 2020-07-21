using Landscape.Base.Entity;
using Landscape.Base.Entity.DTO;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Model;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    //[Authorize] TODO
    //[ODataRoutePrefix("foo")]
    //[ODataRouting()]
    public class MyODataController : ControllerBase
    {
        private readonly ICIModel ciModel;

        public MyODataController(ICIModel ciModel)
        {
            this.ciModel = ciModel;
        }

        public class Test
        {
            public Guid Id { get; set; }
        }

        /// <summary>
        /// list of all CI-IDs
        /// </summary>
        /// <returns></returns>
        [HttpGet("getAllCIIDs")]
        [EnableQuery]
        public async Task<IEnumerable<Test>> GetAllCIIDs()
        {
            return (await ciModel.GetCIIDs(null)).Select(guid => new Test() { Id = guid });
        }

        [HttpGet("getAll")]
        [EnableQuery]
        public ActionResult<IEnumerable<Test>> GetAll([FromODataUri]string apiVersion)
        {
            return new List<Test>();
        }
    }
}
