using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using HetsCommon;
using HetsApi.Helpers;
using HetsApi.Model;
using HetsData.Model;
using Microsoft.AspNetCore.Hosting;

namespace HetsApi.Controllers
{
    /// <summary>
    /// Version Controller
    /// </summary>
    [Route("api")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class VersionController : Controller
    {
        private const string CommitKey = "OPENSHIFT_BUILD_COMMIT";

        private readonly DbAppContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        public VersionController(DbAppContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IHostingEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _env = env;

            // set context data
            User user = UserAccountHelper.GetUser(context, httpContextAccessor.HttpContext);
            _context.SmUserId = user.SmUserId;
            _context.DirectoryName = user.SmAuthorizationDirectory;
            _context.SmUserGuid = user.UserGuid;
            _context.SmBusinessGuid = user.BusinessGuid;
        }

        /// <summary>
        /// Commit id
        /// </summary>
        private string CommitId => _configuration[CommitKey];

        /// <summary>
        /// Get server version information
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("version")]
        public virtual IActionResult GetServerVersionInfo()
        {
            ProductVersionInfo info = new ProductVersionInfo();
            info.ApplicationVersions.Add(GetApplicationVersionInfo());
            info.DatabaseVersions.Add(GetDatabaseVersionInfo());

            string buildVersion = _configuration.GetSection("Constants:Version-Application").Value;
            string dbVersion = _configuration.GetSection("Constants:Version-Database").Value;
            string environment = "";

            if (_env.IsProduction())
            {
                environment = "Production";
            }
            else if (_env.IsStaging())
            {
                environment = "Test";
            }
            else if (_env.IsDevelopment())
            {
                environment = "Development";
            }
            else if (_env.IsEnvironment("Training"))
            {
                environment = "Training";
            }
            else if (_env.IsEnvironment("UAT"))
            {
                environment = "UAT";
            }

            if (info.ApplicationVersions[0] != null)
            {
                info.ApplicationVersions[0].BuildVersion = buildVersion;
                info.ApplicationVersions[0].Environment = environment;
            }

            if (info.DatabaseVersions[0] != null)
            {
                info.DatabaseVersions[0].BuildVersion = dbVersion;
                info.DatabaseVersions[0].Environment = environment;
            }

            return Ok(info);
        }

        /// <summary>
        /// Get server version
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [Route("server/version")]
        public virtual IActionResult GetServerVersion()
        {
            return Ok(GetApplicationVersionInfo());
        }

        /// <summary>
        /// Get database version
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("database/version")]
        public virtual IActionResult GetDatabaseVersion()
        {
            return Ok(GetDatabaseVersionInfo());
        }

        private ApplicationVersionInfo GetApplicationVersionInfo()
        {
            Assembly assembly = GetType().GetTypeInfo().Assembly;
            return assembly.GetApplicationVersionInfo(CommitId);
        }

        private DatabaseVersionInfo GetDatabaseVersionInfo()
        {
            return _context.Database.GetDatabaseVersionInfo();
        }
    }
}
