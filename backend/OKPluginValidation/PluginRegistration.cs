using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using OKPluginValidation.Validation;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Validation.Rules;
using System.Collections.Generic;

namespace OKPluginValidation
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IValidationIssueModel, ValidationIssueModel>();
            sc.AddSingleton<IValidationModel, ValidationModel>();
            sc.AddSingleton<IValidationRule, ValidationRuleNamedCI>();
            sc.AddScoped<IValidationEngine, ValidationEngine>();
            sc.AddScoped<ValidationEngineRunner>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new RecursiveTrait[] {
            ValidationTraits.Validation,
            ValidationTraits.ValidationIssue,
        };

        public override void RegisterHangfireJobRunners()
        {
            RecurringJob.AddOrUpdate<ValidationEngineRunner>(s => s.Run(null), "*/5 * * * * *");
        }
    }
}
