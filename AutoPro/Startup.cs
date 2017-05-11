using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AutoPro.Startup))]
namespace AutoPro
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
