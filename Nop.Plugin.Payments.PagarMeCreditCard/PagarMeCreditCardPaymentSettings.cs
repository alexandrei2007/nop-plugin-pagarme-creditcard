using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.PagarMeCreditCard
{
    public class PagarMeCreditCardPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        /// <summary>
        /// Pagar.me api key
        /// </summary>
        public string ApiKey { get; set; }
        /// <summary>
        /// Pagar.me crypt key
        /// </summary>
        public string CryptKey { get; set; }
        /// <summary>
        /// Pagar.me postback url.
        /// </summary>
        public string PostbackUrl { get; set; }
    }
}
