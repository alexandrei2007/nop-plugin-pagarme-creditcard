using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.PagarMeCreditCard.Services;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tasks;
using PagarMe;
using System;
using System.Text;

namespace Nop.Plugin.Payments.PagarMeCreditCard
{
    public class CheckPaymentTask: ITask
    {
        private readonly ILogger _logger;
        private readonly PagarMeCreditCardPaymentSettings _pagarMeCreditCardPaymentSettings;
        private readonly IPaymentProcessorService _paymentProcessorService;

        public CheckPaymentTask(ILogger logger,
            PagarMeCreditCardPaymentSettings pagarMeCreditCardPaymentSettings,
            IPaymentProcessorService paymentProcessorService)
        {
            this._logger = logger;
            this._pagarMeCreditCardPaymentSettings = pagarMeCreditCardPaymentSettings;
            this._paymentProcessorService = paymentProcessorService;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Execute()
        {
            int pageIndex = 0;
            int pageSize = 10;
            int currentOrderId = 0;

            IPagedList<Order> orders = null;

            PagarMeService.DefaultApiKey = _pagarMeCreditCardPaymentSettings.ApiKey;
            PagarMeService.DefaultEncryptionKey = _pagarMeCreditCardPaymentSettings.CryptKey;

            do
            {
                orders = _paymentProcessorService.GetPendingPaymentOrders(currentOrderId, pageIndex, pageSize);

                foreach (var order in orders)
                {
                    currentOrderId = order.Id;

                    try
                    {
                        // get transaction
                        var transaction = PagarMeService.GetDefaultService().Transactions.Find(order.AuthorizationTransactionId);

                        if (transaction != null)
                        {
                            if (transaction.Status == TransactionStatus.Paid)
                            {
                                _paymentProcessorService.MarkOrderAsPaid(order);
                            }
                            else if (transaction.Status == TransactionStatus.Refused)
                            {
                                _paymentProcessorService.CancelOrder(order);
                            }
                        }
                    }
                    catch (PagarMeException ex)
                    {
                        StringBuilder error = new StringBuilder();
                        error.AppendLine("Http Status: " + ex.Error.HttpStatus.ToString());
                        error.AppendLine("Method: " + ex.Error.Method);
                        error.AppendLine("Url: " + ex.Error.Url);

                        _logger.Error(string.Format("Error while running the '{0}' schedule task. {1}", "Nop.Plugin.Payments.PagarMeCreditCard.CheckPaymentTask", error.ToString()), ex);
                    }
                    catch (Exception ex2)
                    {
                        _logger.Error(string.Format("Error while running the '{0}' schedule task. {1}", "Nop.Plugin.Payments.PagarMeCreditCard.CheckPaymentTask", ex2.Message), ex2);
                    }
                }

            }
            while (orders != null && orders.HasNextPage);
        }

    }
}
