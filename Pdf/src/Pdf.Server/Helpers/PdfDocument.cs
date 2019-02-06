﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Newtonsoft.Json.Linq;

namespace Pdf.Server.Helpers
{    
    public class PdfRequest
    {
        public string Html { get; set; }
        public string PdfFileName { get; set; }
        public string RenderJsUrl { get; set; }
    }    

    /// <summary>
    /// Generates a Pdf Document using html-pdf
    /// </summary>
    public static class PdfDocument
    {
        public static async Task<byte[]> BuildPdf(INodeServices nodeServices, PdfRequest request, 
            bool landscape = false, bool smallMargin = false)
        {
            try
            {
                // validate request
                if (string.IsNullOrEmpty(request.PdfFileName))
                {
                    throw new ArgumentException("Missing PdfFileName");
                }

                if (string.IsNullOrEmpty(request.Html))
                {
                    throw new ArgumentException("Missing Html content");
                }

                string jsUrl = request.RenderJsUrl;
                if (smallMargin) jsUrl = jsUrl.Replace(".js", "SmallMargin.js");
                if (landscape) jsUrl = jsUrl.Replace(".js", "Landscape.js");

                // call report js to generate pdf response                            
                byte[] result = await nodeServices.InvokeAsync<byte[]>(jsUrl, request.Html);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static async Task<int> PageCountPdf(INodeServices nodeServices, PdfRequest request,
            bool landscape = false, bool smallMargin = false)
        {
            try
            {
                // validate request
                if (string.IsNullOrEmpty(request.PdfFileName))
                {
                    throw new ArgumentException("Missing PdfFileName");
                }

                if (string.IsNullOrEmpty(request.Html))
                {
                    throw new ArgumentException("Missing Html content");
                }

                string jsUrl = request.RenderJsUrl;
                if (smallMargin) jsUrl = jsUrl.Replace(".js", "SmallMargin.js");
                if (landscape) jsUrl = jsUrl.Replace(".js", "Landscape.js");

                // call report js to generate pdf response and grab the page count           
                JObject temp = await nodeServices.InvokeAsync<JObject>(jsUrl, request.Html, "true");

                string pageCountString = temp["Number-Of-Pages"].ToString();

                // convert page count to int
                bool tryParse = int.TryParse(pageCountString, out int pageCount);

                if (tryParse)
                {
                    return pageCount;
                }

                return -1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
