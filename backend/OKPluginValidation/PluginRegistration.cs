using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using OKPluginValidation.Validation;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Validation.Rules;
using System.Collections.Generic;
using Omnikeeper.Base.Model.TraitBased;

namespace OKPluginValidation
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<GenericTraitEntityModel<ValidationIssue, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Validation.Validation, string>>();
            sc.AddSingleton<IValidationRule, ValidationRuleNamedCI>();
            sc.AddScoped<IValidationEngine, ValidationEngine>();
            sc.AddScoped<ValidationEngineRunner>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new RecursiveTrait[] {
            GenericTraitEntityHelper.Class2RecursiveTrait<Validation.Validation>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<ValidationIssue>(),
        };

        public override void RegisterHangfireJobRunners()
        {
            RecurringJob.AddOrUpdate<ValidationEngineRunner>(s => s.Run(null), "*/5 * * * * *");
        }
    }
}
