using System.Collections.Generic;
using System;
using System.Net;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.Web;

namespace ServiceBelt
{
    public class MoreHttpHeaders
    {
        public const string ExposeHeaders = "Access-Control-Expose-Headers";

        public const string RequestMethod = "Access-Control-Request-Method";

        public const string RequestHeader = "Access-Control-Request-Header";

        public const string Vary = "Vary";

        public const string Origin = "Origin";

        public const string MaxAge = "Access-Control-Max-Age";
    }

    /// <summary>
    /// Plugin adds support for Cross-origin resource sharing (CORS, see http://www.w3.org/TR/access-control/). CORS allows to access resources from different domain which usually forbidden by origin policy. 
    /// </summary>
    public class CorsFeature : IPlugin
    {
        // See http://www.html5rocks.com/en/tutorials/cors/#toc-handling-a-simple-request
        public const string DefaultMethods = "GET,POST,PUT,DELETE,OPTIONS";
        public const string DefaultHeaders = "Cache-Control,Content-Language,Content-Type,Expires,Last-Modified,Pragma";
        public const string DefaultOrigins = "*";

        private readonly ICollection<string> allowOrigins;
        private readonly string allowMethods;
        private readonly string allowHeaders;
        private readonly bool allowCredentials;
        private readonly bool exposeHeaders;
        private readonly int? maxAgeSeconds;
        private readonly bool autoHandleOptionsRequests;
        private static bool isInstalled = false;

        /// <summary>
        /// Constructor with default giving allowed origin as *, 
        /// allowed methods as GET, POST, PUT, DELETE, OPTIONS request and 
        /// allowed headers as Cache-Control,Content-Language,Content-Type,Expires,Last-Modified,Pragma.
        /// </summary>
        public CorsFeature(
            object allowOrigins = null, 
            string allowMethods = DefaultMethods, 
            string allowHeaders = DefaultHeaders, 
            int? maxAgeSeconds = null, 
            bool allowCredentials = false, 
            bool exposeHeaders = false,
            bool autoHandleOptionsRequests = true)
        {
            if (allowOrigins == null)
                this.allowOrigins = null;
            else
            {
                string allowOriginsString = allowOrigins as string;
                ICollection<string> allowOriginsList = allowOrigins as ICollection<string>;

                if (allowOriginsString != null)
                {
                    if (allowOriginsString == DefaultOrigins)
                        this.allowOrigins = null;
                    else
                        this.allowOrigins = allowOriginsString.Split(',', ';');
                }
                else if (allowOriginsList != null)
                {
                    this.allowOrigins = allowOriginsList;
                }
                else
                    throw new ArgumentException("Must be a string or ICollection<string>", "allowOrigins");
            }

            this.allowMethods = allowMethods;
            this.allowHeaders = allowHeaders;
            this.allowCredentials = allowCredentials;
            this.maxAgeSeconds = maxAgeSeconds;
            this.exposeHeaders = exposeHeaders;
            this.autoHandleOptionsRequests = autoHandleOptionsRequests;
        }

        public void Register(IAppHost appHost)
        {
            if (isInstalled) 
                return;

            isInstalled = true;

            Action<IRequest, IResponse> allowOriginFilter = (httpReq, httpRes) =>
            {
                // Following the flow chart given at http://www.html5rocks.com/static/images/cors_server_flowchart.png

                var origin = httpReq.Headers.Get(MoreHttpHeaders.Origin);

                if (origin == null)
                    // Not a valid CORS request
                    return;

                var methods = httpReq.Headers.Get(MoreHttpHeaders.RequestMethod);
                bool preflight;

                if (methods != null && httpReq.Verb == "OPTIONS")
                {
                    preflight = true;

                    var requestMethod = httpReq.Headers.Get(MoreHttpHeaders.RequestMethod);

                    if (requestMethod == null)
                    {
                        // Not a valid preflight request
                        return;
                    }

                    var requestHeader = httpReq.Headers.Get(MoreHttpHeaders.RequestHeader);

                    if (requestHeader != null && !allowHeaders.Contains(requestHeader))
                    {
                        // Not a valid preflight request
                        return;
                    }

                    httpRes.AddHeader(HttpHeaders.AllowMethods, allowMethods);
                    httpRes.AddHeader(HttpHeaders.AllowHeaders, allowHeaders);

                    if (maxAgeSeconds.HasValue)
                        httpRes.AddHeader(MoreHttpHeaders.MaxAge, maxAgeSeconds.Value.ToString());
                }
                else
                {
                    preflight = false;

                    // Actual request, should response headers be exposed to the client?
                    if (exposeHeaders)
                        httpRes.AddHeader(MoreHttpHeaders.ExposeHeaders, allowHeaders);
                }

                if (this.allowOrigins == null)
                {
                    httpRes.AddHeader(HttpHeaders.AllowOrigin, "*");
                }
                else if (allowOrigins.Contains(origin))
                {
                    httpRes.AddHeader(HttpHeaders.AllowOrigin, origin);
                    // See http://www.w3.org/TR/cors/#resource-implementation
                    httpRes.AddHeader(MoreHttpHeaders.Vary, MoreHttpHeaders.Origin);
                }

                if (allowCredentials)
                {
                    httpRes.AddHeader(HttpHeaders.AllowCredentials, allowCredentials.ToString().ToLower());
                }

                if (preflight)
                {
                    httpRes.AddHeader(HttpHeaders.ContentType, "text/html; charset=utf-8");
                    httpRes.StatusCode = (int)HttpStatusCode.OK;
                    httpRes.Close();
                }
            };

            appHost.PreRequestFilters.Add(allowOriginFilter);

            if (autoHandleOptionsRequests)
            {
                CustomActionHandler handler = new CustomActionHandler((httpReq, httpRes) =>
                {
                    if (allowOriginFilter != null)
                    {
                        allowOriginFilter.Invoke(httpReq, httpRes);
                    }

                    httpRes.EndRequest();
                });

                appHost.RawHttpHandlers.Add((httpReq) => 
                {
                    if (httpReq.Verb != "OPTIONS")
                        return null;

                    return handler;
                });
            }
        }
    }
}