using PagarMe;
using PagarMe.Base;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Models
{
    public class Metadata : AbstractModel
    {
        public Metadata()
            : this(null)
        {
        }

        public Metadata(PagarMeService service)
            : base(service)
        {
        }
        public string OrderGuid
        {
            get { return GetAttribute<string>("orderGuid"); }
            set { SetAttribute("orderGuid", value); }
        }

        public string CustomerEmail
        {
            get { return GetAttribute<string>("customerEmail"); }
            set { SetAttribute("customerEmail", value); }
        }
    }
}
