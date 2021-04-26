using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using HetsApi.Authorization;
using HetsApi.Helpers;
using HetsApi.Model;
using HetsData.Helpers;
using HetsData.Model;

namespace HetsApi.Controllers
{
    /// <summary>
    /// Attachment Upload Controller
    /// </summary>
    [Route("api")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class AttachmentUploadController : Controller
    {
        private readonly DbAppContext _context;

        public AttachmentUploadController(DbAppContext context, IHttpContextAccessor httpContextAccessor)
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
        /// Associate an attachment with a piece of equipment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="files"></param>
        [HttpPost]
        [Route("equipment/{id}/attachments")]
        [SwaggerOperation("EquipmentIdAttachmentsPost")]
        [SwaggerResponse(200, type: typeof(List<HetDigitalFile>))]
        [RequiresPermission(HetPermission.Login, HetPermission.WriteAccess)]
        public virtual IActionResult EquipmentIdAttachmentsPost([FromRoute] int id, [FromForm]IList<IFormFile> files)
        {
            // validate the id
            bool exists = _context.HetEquipment.Any(a => a.EquipmentId == id);

            if (!exists) return new StatusCodeResult(404);

            HetEquipment equipment = _context.HetEquipment
                .Include(x => x.HetDigitalFile)
                .First(a => a.EquipmentId == id);

            foreach (IFormFile file in files)
            {
                if (file.Length > 0)
                {
                    HetDigitalFile attachment = new HetDigitalFile();

                    // strip out extra info in the file name
                    if (!string.IsNullOrEmpty(file.FileName))
                    {
                        attachment.FileName = Path.GetFileName(file.FileName);
                    }

                    // allocate storage for the file and create a memory stream
                    attachment.FileContents = new byte[file.Length];

                    using (MemoryStream fileStream = new MemoryStream(attachment.FileContents))
                    {
                        file.CopyTo(fileStream);
                    }

                    attachment.Type = GetType(attachment.FileName);

                    // set the mime type id
                    int? mimeTypeId = StatusHelper.GetMimeTypeId(attachment.Type, _context);
                    if (mimeTypeId == null) throw new DataException("Mime Type Id cannot be null");

                    attachment.MimeTypeId = (int)mimeTypeId;

                    equipment.HetDigitalFile.Add(attachment);
                }
            }

            _context.SaveChanges();

            return new ObjectResult(equipment.HetDigitalFile);
        }

        /// <summary>
        /// Get attachments associated with a piece of equipment
        /// </summary>
        /// <param name="id"></param>
        [HttpGet]
        [Route("equipment/{id}/attachmentsForm")]
        [Produces("text/html")]
        public virtual IActionResult EquipmentIdAttachmentsFormGet([FromRoute] int id)
        {
            return new ObjectResult("<html><body><form method=\"post\" action=\"/api/equipment/" + id + "/attachments\" enctype=\"multipart/form-data\"><input type=\"file\" name = \"files\" multiple /><input type = \"submit\" value = \"Upload\" /></body></html>");
        }

        /// <summary>
        /// Associate an attachment with a project
        /// </summary>
        /// <param name="id"></param>
        /// <param name="files"></param>
        [HttpPost]
        [Route("projects/{id}/attachments")]
        [SwaggerOperation("ProjectIdAttachmentsPost")]
        [SwaggerResponse(200, type: typeof(List<HetDigitalFile>))]
        [RequiresPermission(HetPermission.Login, HetPermission.WriteAccess)]
        public virtual IActionResult ProjectIdAttachmentsPost([FromRoute] int id, [FromForm] IList<IFormFile> files)
        {
            // validate the id
            bool exists = _context.HetProject.Any(a => a.ProjectId == id);

            if (!exists) return new StatusCodeResult(404);

            HetProject project = _context.HetProject
                .Include(x => x.HetDigitalFile)
                .First(a => a.ProjectId == id);

            foreach (IFormFile file in files)
            {
                if (file.Length > 0)
                {
                    HetDigitalFile attachment = new HetDigitalFile();

                    // strip out extra info in the file name
                    if (!string.IsNullOrEmpty(file.FileName))
                    {
                        attachment.FileName = Path.GetFileName(file.FileName);
                    }

                    // allocate storage for the file and create a memory stream
                    attachment.FileContents = new byte[file.Length];

                    using (MemoryStream fileStream = new MemoryStream(attachment.FileContents))
                    {
                        file.CopyTo(fileStream);
                    }

                    attachment.Type = GetType(attachment.FileName);

                    // set the mime type id
                    int? mimeTypeId = StatusHelper.GetMimeTypeId(attachment.Type, _context);
                    if (mimeTypeId == null) throw new DataException("Mime Type Id cannot be null");

                    attachment.MimeTypeId = (int)mimeTypeId;

                    project.HetDigitalFile.Add(attachment);
                }
            }

            _context.SaveChanges();

            return new ObjectResult(project.HetDigitalFile);
        }

        /// <summary>
        /// Get attachments associated with a project
        /// </summary>
        /// <param name="id"></param>
        [HttpGet]
        [Route("projects/{id}/attachmentsForm")]
        [Produces("text/html")]
        [RequiresPermission(HetPermission.Login)]
        public virtual IActionResult ProjectIdAttachmentsFormGet([FromRoute] int id)
        {
            return new ObjectResult("<html><body><form method=\"post\" action=\"/api/projects/" + id + "/attachments\" enctype=\"multipart/form-data\"><input type=\"file\" name = \"files\" multiple /><input type = \"submit\" value = \"Upload\" /></body></html>");
        }

        /// <summary>
        /// Associate an owner with an attachment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="files"></param>
        [HttpPost]
        [Route("owners/{id}/attachments")]
        [SwaggerOperation("OwnerIdAttachmentsPost")]
        [SwaggerResponse(200, type: typeof(List<HetDigitalFile>))]
        [RequiresPermission(HetPermission.Login, HetPermission.WriteAccess)]
        public virtual IActionResult OwnerIdAttachmentsPost([FromRoute] int id, [FromForm] IList<IFormFile> files)
        {
            // validate the id
            bool exists = _context.HetOwner.Any(a => a.OwnerId == id);

            if (!exists) return new StatusCodeResult(404);

            HetOwner owner = _context.HetOwner
                .Include(x => x.HetDigitalFile)
                .First(a => a.OwnerId == id);

            foreach (IFormFile file in files)
            {
                if (file.Length > 0)
                {
                    HetDigitalFile attachment = new HetDigitalFile();

                    // strip out extra info in the file name
                    if (!string.IsNullOrEmpty(file.FileName))
                    {
                        attachment.FileName = Path.GetFileName(file.FileName);
                    }

                    // allocate storage for the file and create a memory stream
                    attachment.FileContents = new byte[file.Length];

                    using (MemoryStream fileStream = new MemoryStream(attachment.FileContents))
                    {
                        file.CopyTo(fileStream);
                    }

                    attachment.Type = GetType(attachment.FileName);

                    // set the mime type id
                    int? mimeTypeId = StatusHelper.GetMimeTypeId(attachment.Type, _context);
                    if (mimeTypeId == null) throw new DataException("Mime Type Id cannot be null");

                    attachment.MimeTypeId = (int)mimeTypeId;

                    owner.HetDigitalFile.Add(attachment);
                }
            }

            _context.SaveChanges();

            return new ObjectResult(owner.HetDigitalFile);
        }

        /// <summary>
        /// Get attachments associated with an owner
        /// </summary>
        /// <param name="id"></param>
        [HttpGet]
        [Route("owners/{id}/attachmentsForm")]
        [Produces("text/html")]
        [RequiresPermission(HetPermission.Login)]
        public virtual IActionResult OwnerIdAttachmentsFormGet([FromRoute] int id)
        {
            return new ObjectResult("<html><body><form method=\"post\" action=\"/api/owners/" + id + "/attachments\" enctype=\"multipart/form-data\"><input type=\"file\" name = \"files\" multiple /><input type = \"submit\" value = \"Upload\" /></body></html>");
        }

        /// <summary>
        /// Associate an attachment with a rental request
        /// </summary>
        /// <param name="id"></param>
        /// <param name="files"></param>
        [HttpPost]
        [Route("rentalRequests/{id}/attachments")]
        [SwaggerOperation("RentalRequestIdAttachmentsPost")]
        [SwaggerResponse(200, type: typeof(List<HetDigitalFile>))]
        [RequiresPermission(HetPermission.Login, HetPermission.WriteAccess)]
        public virtual IActionResult RentalRequestIdAttachmentsPost([FromRoute] int id, [FromForm] IList<IFormFile> files)
        {
            // validate the id
            bool exists = _context.HetRentalRequest.Any(a => a.RentalRequestId == id);

            if (!exists) return new StatusCodeResult(404);

            HetRentalRequest rentalRequest = _context.HetRentalRequest
                .Include(x => x.HetDigitalFile)
                .First(a => a.RentalRequestId == id);

            foreach (IFormFile file in files)
            {
                if (file.Length > 0)
                {
                    HetDigitalFile attachment = new HetDigitalFile();

                    // strip out extra info in the file name
                    if (!string.IsNullOrEmpty(file.FileName))
                    {
                        attachment.FileName = Path.GetFileName(file.FileName);
                    }

                    // allocate storage for the file and create a memory stream
                    attachment.FileContents = new byte[file.Length];

                    using (MemoryStream fileStream = new MemoryStream(attachment.FileContents))
                    {
                        file.CopyTo(fileStream);
                    }

                    attachment.Type = GetType(attachment.FileName);

                    // set the mime type id
                    int? mimeTypeId = StatusHelper.GetMimeTypeId(attachment.Type, _context);
                    if (mimeTypeId == null) throw new DataException("Mime Type Id cannot be null");

                    attachment.MimeTypeId = (int)mimeTypeId;

                    rentalRequest.HetDigitalFile.Add(attachment);
                }
            }

            _context.SaveChanges();

            return new ObjectResult(rentalRequest.HetDigitalFile);
        }

        /// <summary>
        /// Get attachments associated with  rental request
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("rentalRequests/{id}/attachmentsForm")]
        [Produces("text/html")]
        [RequiresPermission(HetPermission.Login)]
        public virtual IActionResult RentalRequestIdAttachmentsFormGet([FromRoute] int id)
        {
            return new ObjectResult("<html><body><form method=\"post\" action=\"/api/rentalRequests/" + id + "/attachments\" enctype=\"multipart/form-data\"><input type=\"file\" name = \"files\" multiple /><input type = \"submit\" value = \"Upload\" /></body></html>");
        }

        #region Get File Extension

        private string GetType(string fileName)
        {
            // get extension
            int extStart = fileName.LastIndexOf('.');

            if (extStart > 0)
            {
                return fileName.Substring(extStart + 1).ToLower();
            }

            return "";
        }

        #endregion
    }
}
