using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(CDE.Startup))]

namespace CDE
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
