using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Tasks;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.PagarMeCreditCard.Controllers;
using Nop.Plugin.Payments.PagarMeCreditCard.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tasks;
using PagarMe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Routing;

namespace Nop.Plugin.Payments.PagarMeCreditCard
{
    /// <summary>
    /// PagarMeCreditCard payment processor
    /// </summary>
    public class PagarMeCreditCardPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly PagarMeCreditCardPaymentSettings _pagarMeCreditCardPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICustomerService _customerService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IWebHelper _webHelper;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public PagarMeCreditCardPaymentProcessor(PagarMeCreditCardPaymentSettings pagarMeCreditCardPaymentSettings,
            ISettingService settingService,
            ICustomerService customerService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IWebHelper webHelper,
            ILogger logger,
            IScheduleTaskService scheduleTaskService)
        {
            this._pagarMeCreditCardPaymentSettings = pagarMeCreditCardPaymentSettings;
            this._settingService = settingService;
            this._customerService = customerService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._webHelper = webHelper;
            this._logger = logger;
            this._scheduleTaskService = scheduleTaskService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// https://docs.pagar.me/transactions/#realizando-uma-transacao-de-cartao-de-credito
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var total = processPaymentRequest.OrderTotal;

            string postbackUrl;
            if (string.IsNullOrWhiteSpace(_pagarMeCreditCardPaymentSettings.PostbackUrl))
                postbackUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentPagarMeCreditCard/Callback/" + processPaymentRequest.OrderGuid.ToString();
            else
                postbackUrl = _pagarMeCreditCardPaymentSettings.PostbackUrl;

            PagarMeService.DefaultApiKey = _pagarMeCreditCardPaymentSettings.ApiKey;
            PagarMeService.DefaultEncryptionKey = _pagarMeCreditCardPaymentSettings.CryptKey;

            try
            {
                // format: mmyy
                var expiration = processPaymentRequest.CreditCardExpireMonth.ToString().PadLeft(2, '0') + processPaymentRequest.CreditCardExpireYear.ToString().Substring(2, 2);

                CardHash card = new CardHash();
                card.CardNumber = processPaymentRequest.CreditCardNumber;
                card.CardHolderName = processPaymentRequest.CreditCardName;
                card.CardExpirationDate = expiration;
                card.CardCvv = processPaymentRequest.CreditCardCvv2;

                string cardhash = card.Generate();

                // save transaction
                Transaction transaction = new Transaction();
                transaction.Amount = FormatAmount(total);
                transaction.CardHash = cardhash;
                transaction.PaymentMethod = PagarMe.PaymentMethod.CreditCard;
                transaction.PostbackUrl = postbackUrl;

                SetMetadata(transaction, processPaymentRequest);

                transaction.Save();

                // result
                var result = new ProcessPaymentResult();
                result.NewPaymentStatus = PaymentStatus.Pending;
                result.AuthorizationTransactionId = transaction.Id;

                return result;
            }
            catch (PagarMe.PagarMeException ex)
            {
                bool shouldLog = true;

                if (ex.Error != null && ex.Error.Errors != null && ex.Error.Errors.Count() == 1 && ex.Error.Errors[0].Parameter == "card_expiration_date")
                    shouldLog = false;

                if (shouldLog)
                {
                    _logger.Error(ex.Message, ex);
                }

                if (ex.Error != null && ex.Error.Errors != null && ex.Error.Errors.Length > 0)
                    throw new NopException(ex.Error.Errors[0].Message);
                else
                    throw new NopException("Não foi possível processar o pagamento");
            }
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country

            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _pagarMeCreditCardPaymentSettings.AdditionalFee, _pagarMeCreditCardPaymentSettings.AdditionalFeePercentage);

            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();

            try
            {
                PagarMeService.DefaultApiKey = _pagarMeCreditCardPaymentSettings.ApiKey;
                PagarMeService.DefaultEncryptionKey = _pagarMeCreditCardPaymentSettings.CryptKey;

                var transaction = PagarMeService.GetDefaultService().Transactions.Find(refundPaymentRequest.Order.AuthorizationTransactionId);

                if (transaction != null)
                {
                    if (transaction.Status == TransactionStatus.Paid)
                    {
                        transaction.Refund();
                        result.NewPaymentStatus = PaymentStatus.Refunded;
                    }
                    else
                    {
                        result.AddError("Transação não pode ser estornada");
                    }
                }
                else
                {
                    result.AddError("Transação não encontrada");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);

                result.AddError("Ocorreu um erro ao processar o estorno");
            }

            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentPagarMeCreditCard";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.PagarMeCreditCard.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentPagarMeCreditCard";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.PagarMeCreditCard.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentPagarMeCreditCardController);
        }

        public override void Install()
        {
            //settings
            var settings = new PagarMeCreditCardPaymentSettings
            {
                ApiKey = "",
                CryptKey = "",
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFee.Hint", "The additional fee.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ShippableProductRequired", "Shippable product required");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ShippableProductRequired.Hint", "An option indicating whether shippable products are required in order to display this payment method during checkout.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ApiKey", "Api key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ApiKey.Hint", "Your api key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.CryptKey", "Crypt key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.CryptKey.Hint", "Your crypt key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.PostbackUrl", "Postback Url");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.PostbackUrl.Hint", "Postback Url");
            this.AddOrUpdatePluginLocaleResource("Plugins.FriendlyName.Payments.PagarMeCreditCard", "Cartão de Crédito");

            //create task for check payments
            var task = new ScheduleTask
            {
                Name = "PagarMeCreditCard check payments",
                Seconds = 3600,
                Type = "Nop.Plugin.Payments.PagarMeCreditCard.CheckPaymentTask, Nop.Plugin.Payments.PagarMeCreditCard",
                Enabled = true,
                StopOnError = false
            };

            _scheduleTaskService.InsertTask(task);

            base.Install();
        }
        
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<PagarMeCreditCardPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ShippableProductRequired");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ShippableProductRequired.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ApiKey");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.ApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.CryptKey");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.CryptKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.PostbackUrl");
            this.DeletePluginLocaleResource("Plugins.Payment.PagarMeCreditCard.PostbackUrl.Hint");
            this.DeletePluginLocaleResource("Plugins.FriendlyName.Payments.PagarMeCreditCard");

            //task
            var task = _scheduleTaskService.GetTaskByType("Nop.Plugin.Payments.PagarMeCreditCard.CheckPaymentTask, Nop.Plugin.Payments.PagarMeCreditCard");
            if (task != null)
            {
                _scheduleTaskService.DeleteTask(task);
            }

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Formata o valor para ser usado na api Pagar.me.
        /// e.g. R$ 11,99 = 1199
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        protected static int FormatAmount(decimal amount)
        {
            return Convert.ToInt32(amount.ToString("f2", CultureInfo.InvariantCulture).Replace(".", ""));
        }

        /// <summary>
        /// Set transaction metadata.
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="processPaymentRequest"></param>
        private void SetMetadata(Transaction transaction, ProcessPaymentRequest processPaymentRequest)
        {
            string customerEmail = string.Empty;
            string orderGuid = string.Empty;

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            if (customer != null)
                customerEmail = customer.Email;

            if (processPaymentRequest.OrderGuid != null)
                orderGuid = processPaymentRequest.OrderGuid.ToString();

            transaction.Metadata = new Metadata()
            {
                OrderGuid = orderGuid,
                CustomerEmail = customerEmail
            };
        }

        #endregion

    }
}
