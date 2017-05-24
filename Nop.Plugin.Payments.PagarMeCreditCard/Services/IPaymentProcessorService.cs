using Nop.Core;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Services
{
    public interface IPaymentProcessorService
    {
        IPagedList<Order> GetPendingPaymentOrders(int fromOrderId, int pageIndex = 0, int pageSize = int.MaxValue);
        void CancelOrder(Order order);
        void MarkOrderAsPaid(Order order);
    }
}
