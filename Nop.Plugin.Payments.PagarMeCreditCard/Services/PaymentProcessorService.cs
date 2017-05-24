using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Orders;
using System;
using System.Linq;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Services
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        #region Properties

        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IRepository<Order> _orderRepository;
        private readonly IOrderService _orderService;

        #endregion

        #region Contructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderProcessingService"></param>
        /// <param name="orderService"></param>
        /// <param name="orderRepository"></param>
        public PaymentProcessorService(IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IRepository<Order> orderRepository)
        {
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._orderRepository = orderRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromOrderId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public IPagedList<Order> GetPendingPaymentOrders(int fromOrderId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _orderRepository.Table
                .Where(o =>
                    o.Id > fromOrderId &&
                    o.PaymentMethodSystemName == "Payments.PagarMeCreditCard" &&
                    o.OrderStatusId == (int)OrderStatus.Pending &&
                    o.PaymentStatusId == (int)PaymentStatus.Pending)
                .OrderBy(o => o.Id);

            return new PagedList<Order>(query, pageIndex, pageSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public void CancelOrder(Order order)
        {
            if (_orderProcessingService.CanCancelOrder(order))
            {
                //order note
                order.OrderNotes.Add(new OrderNote
                {
                    Note = "Payment refused",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                _orderService.UpdateOrder(order);

                _orderProcessingService.CancelOrder(order, true);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="order"></param>
        public void MarkOrderAsPaid(Order order)
        {
            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                //order note
                order.OrderNotes.Add(new OrderNote
                {
                    Note = "Payment confirmed",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                _orderService.UpdateOrder(order);

                _orderProcessingService.MarkOrderAsPaid(order);
            }
        }

        #endregion
    }
}
