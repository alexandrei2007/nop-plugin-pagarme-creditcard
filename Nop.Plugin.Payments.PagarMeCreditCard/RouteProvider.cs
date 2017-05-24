using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.PagarMeCreditCard
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Callback
            routes.MapRoute("Plugin.Payments.PagarMeCreditCard.Callback",
                 "Plugins/PaymentPagarMeCreditCard/Callback/{orderId}",
                 new { controller = "PaymentPagarMeCreditCard", action = "Callback" },
                 new[] { "Nop.Plugin.Payments.PagarMeCreditCard.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
