using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using HetsApi.Helpers;
using HetsApi.Model;
using HetsData.Model;
using Microsoft.AspNetCore.Authorization;

namespace HetsApi.Controllers
{
    /// <summary>
    /// Service Areas Controller
    /// </summary>
    [Route("api/serviceAreas")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ServiceAreaController : Controller
    {
        private readonly DbAppContext _context;

        public ServiceAreaController(DbAppContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;

            // set context data
            User user = UserAccountHelper.GetUser(context, httpContextAccessor.HttpContext);
            _context.SmUserId = user.SmUserId;
            _context.DirectoryName = user.SmAuthorizationDirectory;
            _context.SmUserGuid = user.UserGuid;
            _context.SmBusinessGuid = user.BusinessGuid;
        }

        /// <summary>
        /// Get all service areas
        /// </summary>
        [HttpGet]
        [Route("")]
        [SwaggerOperation("ServiceAreasGet")]
        [SwaggerResponse(200, type: typeof(List<HetServiceArea>))]
        [AllowAnonymous]
        public virtual IActionResult ServiceAreasGet()
        {
            List<HetServiceArea> serviceAreas = _context.HetServiceArea
                .Include(x => x.District.Region)
                .ToList();

            return new ObjectResult(new HetsResponse(serviceAreas));
        }
    }
}
