﻿using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using HetsApi.Model;

namespace HetsApi.Controllers
{
    /// <summary>
    /// Error Controller for HetsApi
    /// </summary>
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        private readonly IHostingEnvironment _env;

        /// <summary>
        /// Error Controller Constructor
        /// </summary>
        /// <param name="env"></param>
        public ErrorController(IHostingEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Default action
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult Index()
        {
            HomeModel home = new HomeModel();

            // if we don't have a context - exit
            if (HttpContext == null) return View(home);

            // retrieve exception from the http context
            IExceptionHandlerFeature feature = HttpContext.Features.Get<IExceptionHandlerFeature>();

            // *****************************************
            // get original target url
            // *****************************************
            string originalUrl = "";

            try
            {
                IHttpRequestFeature httpRequestFeature = HttpContext.Features.Get<IHttpRequestFeature>();
                originalUrl = httpRequestFeature.RawTarget;
            }
            catch
            {
                // do nothing
            }

            // *****************************************
            // check if this is a HETS Exception message
            // *****************************************
            string source = "";

            PropertyInfo pi = feature?.Error.GetType().GetProperty("SourceMethod");

            if (pi != null)
            {
                string tempValue = (string)pi.GetValue(feature.Error, null);
                if (!string.IsNullOrEmpty(tempValue))
                {
                    source+= "." + tempValue;
                }
            }

            // *****************************************
            // check if we have an inner exception
            // *****************************************
            string innerException = "";

            if (feature?.Error.InnerException != null)
            {
                pi = feature.Error.InnerException.GetType().GetProperty("Message");
                if (pi != null)
                {
                    string tempValue = (string) pi.GetValue(feature.Error.InnerException, null);
                    if (!string.IsNullOrEmpty(tempValue))
                    {
                        innerException = tempValue;
                    }
                }
            }

            home.DevelopmentEnvironment = _env.IsDevelopment();
            home.UserId = HttpContext.User.Identity.Name;
            home.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            home.Message = feature?.Error.Message;
            home.Source = source;
            home.InnerMessage = innerException;
            home.OriginalUrl = originalUrl;

            return View(home);
        }
    }
}