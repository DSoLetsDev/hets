﻿using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace HetsCommon
{
    public static class WebExtensions
    {
        public static string ToHtml(this IHeaderDictionary headers)
        {
            return HeadersToHtml(headers);
        }

        private static string HeadersToHtml(IHeaderDictionary headers)
        {
            StringBuilder html = new StringBuilder();

            html.AppendLine("<b>Request Headers:</b>");
            html.AppendLine("<ul style=\"list-style-type:none\">");

            foreach (var item in headers)
            {
                html.AppendFormat("<li><b>{0}</b> = {1}</li>\r\n", item.Key, ExpandValue(item.Value));
            }

            html.AppendLine("</ul>");
            return html.ToString();
        }

        /// <summary>
        /// Utility function used to expand headers.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private static string ExpandValue(IEnumerable<string> values)
        {
            StringBuilder value = new StringBuilder();

            foreach (string item in values)
            {
                if (value.Length > 0)
                {
                    value.Append(", ");
                }
                value.Append(item);
            }

            return value.ToString();
        }
    }
}
