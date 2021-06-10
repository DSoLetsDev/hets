using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Annotations;
using HetsApi.Authorization;
using HetsApi.Helpers;
using HetsApi.Model;
using HetsData.Model;

namespace HetsApi.Controllers
{
    /// <summary>
    /// Equipment Types Controller
    /// </summary>
    [Route("api/equipmentTypes")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class EquipmentTypeController : Controller
    {
        private readonly DbAppContext _context;

        public EquipmentTypeController(DbAppContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
        }

        /// <summary>
        /// Get all equipment types
        /// </summary>
        [HttpGet]
        [Route("")]
        [SwaggerOperation("EquipmentTypesGet")]
        [SwaggerResponse(200, type: typeof(List<HetEquipmentType>))]
        [RequiresPermission(HetPermission.Login)]
        public virtual IActionResult EquipmentTypesGet()
        {
            List<HetEquipmentType> equipmentTypes = _context.HetEquipmentType.ToList();

            return new ObjectResult(new HetsResponse(equipmentTypes));
        }
    }
}
