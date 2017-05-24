using Nop.Core;
using Nop.Plugin.Payments.PagarMeCreditCard.Infrastructure;
using Nop.Plugin.Payments.PagarMeCreditCard.Models;
using Nop.Plugin.Payments.PagarMeCreditCard.Services;
using Nop.Plugin.Payments.PagarMeCreditCard.Validators;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Controllers
{
    public class PaymentPagarMeCreditCardController : BasePaymentController
    {
        private readonly PagarMeCreditCardPaymentSettings _pagarMeCreditCardPaymentSettings;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IPaymentProcessorService _paymentProcessorService;

        public PaymentPagarMeCreditCardController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            ILogger logger,
            IOrderService orderService,
            PagarMeCreditCardPaymentSettings pagarMeCreditCardPaymentSettings,
            IPaymentProcessorService paymentProcessorService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderService = orderService;
            this._pagarMeCreditCardPaymentSettings = pagarMeCreditCardPaymentSettings;
            this._paymentProcessorService = paymentProcessorService;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var pagarMeCreditCardPaymentSettings = _settingService.LoadSetting<PagarMeCreditCardPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.AdditionalFee = pagarMeCreditCardPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = pagarMeCreditCardPaymentSettings.AdditionalFeePercentage;
            model.ApiKey = pagarMeCreditCardPaymentSettings.ApiKey;
            model.CryptKey = pagarMeCreditCardPaymentSettings.CryptKey;
            model.PostbackUrl = pagarMeCreditCardPaymentSettings.PostbackUrl;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(pagarMeCreditCardPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(pagarMeCreditCardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ApiKey_OverrideForStore = _settingService.SettingExists(pagarMeCreditCardPaymentSettings, x => x.ApiKey, storeScope);
                model.CryptKey_OverrideForStore = _settingService.SettingExists(pagarMeCreditCardPaymentSettings, x => x.CryptKey, storeScope);
                model.PostbackUrl_OverrideForStore = _settingService.SettingExists(pagarMeCreditCardPaymentSettings, x => x.PostbackUrl, storeScope);
            }

            return View("~/Plugins/Payments.PagarMeCreditCard/Views/PaymentPagarMeCreditCard/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var pagarMeCreditCardPaymentSettings = _settingService.LoadSetting<PagarMeCreditCardPaymentSettings>(storeScope);

            //save settings
            pagarMeCreditCardPaymentSettings.AdditionalFee = model.AdditionalFee;
            pagarMeCreditCardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            pagarMeCreditCardPaymentSettings.ApiKey = model.ApiKey;
            pagarMeCreditCardPaymentSettings.CryptKey = model.CryptKey;
            pagarMeCreditCardPaymentSettings.PostbackUrl = model.PostbackUrl;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.AdditionalFee_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pagarMeCreditCardPaymentSettings, x => x.AdditionalFee, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pagarMeCreditCardPaymentSettings, x => x.AdditionalFee, storeScope);

            if (model.AdditionalFeePercentage_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pagarMeCreditCardPaymentSettings, x => x.AdditionalFeePercentage, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pagarMeCreditCardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);

            if (model.ApiKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pagarMeCreditCardPaymentSettings, x => x.ApiKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pagarMeCreditCardPaymentSettings, x => x.ApiKey, storeScope);

            if (model.CryptKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pagarMeCreditCardPaymentSettings, x => x.CryptKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pagarMeCreditCardPaymentSettings, x => x.CryptKey, storeScope);

            if (model.PostbackUrl_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(pagarMeCreditCardPaymentSettings, x => x.PostbackUrl, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(pagarMeCreditCardPaymentSettings, x => x.PostbackUrl, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            
            var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));
            if (selectedYear != null)
                selectedYear.Selected = true;

            return View("~/Plugins/Payments.PagarMeCreditCard/Views/PaymentPagarMeCreditCard/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };

            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            return paymentInfo;
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Callback(string orderId)
        {
            try
            {
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderId);
                }
                catch
                {
                }

                string fingerprint = Request["fingerprint"];
                string id = Request["id"];
                string currentStatus = Request["current_status"];

                bool isValid = this.IsFingerprintValid(id, fingerprint);

                if (isValid)
                {
                    var order = _orderService.GetOrderByGuid(orderNumberGuid);

                    if (!string.IsNullOrEmpty(currentStatus))
                    {
                        // obter a ordem
                        if (order != null)
                        {
                            if (currentStatus == "paid")
                            {
                                _paymentProcessorService.MarkOrderAsPaid(order);
                            }
                            else if (currentStatus == "refused")
                            {
                                _paymentProcessorService.CancelOrder(order);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Payments.PagarMeCreditCard callback error", ex);
                _logger.Error(GetLogError("Payments.PagarMeCreditCard callback error", Request.Form, orderId));
            }

            return Content("");
        }

        /// <summary>
        /// https://pagar.me/docs/advanced/#validando-a-origem-de-um-postback
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="fingerprint"></param>
        /// <returns></returns>
        [NonAction]
        protected bool IsFingerprintValid(string objectId, string fingerprint)
        {
            if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(fingerprint))
                return false;

            string key = objectId + "#" + _pagarMeCreditCardPaymentSettings.ApiKey;
            string hash = Sha1Cryptography.CreateHash(key);

            return (fingerprint == hash);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="form"></param>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [NonAction]
        protected string GetLogError(string message, NameValueCollection form, string orderId)
        {
            StringBuilder logDescription = new StringBuilder();
            logDescription.AppendLine(message);
            logDescription.AppendFormat("Order id = {0}", orderId).AppendLine();

            foreach (var key in form.Keys)
            {
                logDescription.AppendFormat("{0}: {1}", key.ToString(), form[key.ToString()]).AppendLine();
            }

            return logDescription.ToString();
        }
    }
}