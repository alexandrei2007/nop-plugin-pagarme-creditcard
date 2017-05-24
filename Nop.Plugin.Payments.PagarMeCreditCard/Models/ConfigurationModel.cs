using FluentValidation.Attributes;
using Nop.Plugin.Payments.PagarMeCreditCard.Validators;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using System.Web.Mvc;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Models
{
    [Validator(typeof(ConfigurationValidator))]
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payment.PagarMeCreditCard.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payment.PagarMeCreditCard.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [AllowHtml]
        [NopResourceDisplayName("Plugins.Payment.PagarMeCreditCard.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [AllowHtml]
        [NopResourceDisplayName("Plugins.Payment.PagarMeCreditCard.CryptKey")]
        public string CryptKey { get; set; }
        public bool CryptKey_OverrideForStore { get; set; }

        [AllowHtml]
        [NopResourceDisplayName("Plugins.Payment.PagarMeCreditCard.PostbackUrl")]
        public string PostbackUrl { get; set; }
        public bool PostbackUrl_OverrideForStore { get; set; }

    }
}