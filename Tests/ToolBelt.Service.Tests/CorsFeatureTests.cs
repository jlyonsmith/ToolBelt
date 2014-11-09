using NUnit.Framework;
using System;
using ToolBelt.Service;
using ServiceStack;
using ServiceStack.Web;
using System.Collections.Generic;
using System.Web;

namespace ToolBelt.ServiceStack.Tests
{
    class TestAppHost : IAppHost
    {
        List<Action<IRequest, IResponse>>  preRequestFilters = new List<Action<IRequest, IResponse>>();
        List<Func<IHttpRequest, IHttpHandler>> rawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>>();

        #region IAppHost

        public string ResolveLocalizedString(string text, IRequest request)
        {
            throw new NotImplementedException();
        }

        public global::ServiceStack.Configuration.IAppSettings AppSettings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public global::ServiceStack.Host.ServiceMetadata Metadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Action<IAppHost>> AfterInitCallbacks
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public void Register<T>(T instance)
        {
            throw new NotImplementedException();
        }
        public void RegisterAs<T, TAs>() where T : TAs
        {
            throw new NotImplementedException();
        }
        public void Release(object instance)
        {
            throw new NotImplementedException();
        }
        public void OnEndRequest()
        {
            throw new NotImplementedException();
        }
        public void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            throw new NotImplementedException();
        }
        public void LoadPlugin(params IPlugin[] plugins)
        {
            throw new NotImplementedException();
        }
        public IServiceRunner<TRequest> CreateServiceRunner<TRequest>(global::ServiceStack.Host.ActionContext actionContext)
        {
            throw new NotImplementedException();
        }
        public string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            throw new NotImplementedException();
        }
        public string ResolveLocalizedString(string text)
        {
            throw new NotImplementedException();
        }
        public IServiceRoutes Routes
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public IContentTypes ContentTypes
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Action<IRequest, IResponse>> PreRequestFilters
        {
            get
            {
                return preRequestFilters;
            }
        }
        public List<Action<IRequest, IResponse, object>> GlobalRequestFilters
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<global::ServiceStack.Html.IViewEngine> ViewEngines
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<global::ServiceStack.Host.HandleServiceExceptionDelegate> ServiceExceptionHandlers
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<global::ServiceStack.Host.HandleUncaughtExceptionDelegate> UncaughtExceptionHandlers
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers
        {
            get
            {
                return rawHttpHandlers;
            }
        }
        public List<global::ServiceStack.Host.HttpHandlerResolverDelegate> CatchAllHandlers
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public global::ServiceStack.Host.Handlers.IServiceStackHandler GlobalHtmlErrorHttpHandler
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public Dictionary<System.Net.HttpStatusCode, global::ServiceStack.Host.Handlers.IServiceStackHandler> CustomErrorHttpHandlers
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public Dictionary<Type, Func<IRequest, object>> RequestBinders
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public HostConfig Config
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public List<IPlugin> Plugins
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public global::ServiceStack.IO.IVirtualPathProvider VirtualPathProvider
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region IResolver 
        public T TryResolve<T>()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    [TestFixture()]
    public class CorsFeatureTests
    {
        [Test()]
        public void TestCorsFeature()
        {
            var corsFeature = new ToolBelt.Service.CorsFeature(
                allowOrigins: "http://localhost:1337",
                allowHeaders: ToolBelt.Service.CorsFeature.DefaultHeaders + ",Authorization,Accept,X-Requested-With",
                exposeHeaders: true,
                allowCredentials: true
            );

            var appHost = new TestAppHost();

            Assert.NotNull(appHost.PreRequestFilters);

            corsFeature.Register(appHost);

            Assert.AreEqual(1, appHost.PreRequestFilters.Count);

            // TODO: Drive the filter according the to flow chart at 
            // http://www.html5rocks.com/static/images/cors_server_flowchart.png
        }
    }
}

