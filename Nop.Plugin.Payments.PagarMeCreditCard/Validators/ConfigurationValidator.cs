using FluentValidation;
using Nop.Plugin.Payments.PagarMeCreditCard.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.PagarMeCreditCard.Validators
{
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.ApiKey).NotEmpty().WithMessage("Informe a api key");
            RuleFor(x => x.CryptKey).NotEmpty().WithMessage("Informe a crypt key");
        }
    }
}