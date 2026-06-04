using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
// using System.Web.Optimization; // Removed: not needed
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace CDE
{
    public partial class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            // BundleConfig.RegisterBundles(BundleTable.Bundles); // Removed: requires Optimization package
        }
    }
}
