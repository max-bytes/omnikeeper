using Microsoft.Extensions.DependencyInjection;
using OKPluginValidation.Rules;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using System.Collections.Generic;

namespace OKPluginValidation
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<ValidationIssueModel>();
            sc.AddSingleton<ValidationModel>();
            sc.AddScoped<IValidationEngine, ValidationEngine>();
            //sc.AddScoped<ValidationEngineRunner>();

            sc.AddSingleton<IValidationRule, ValidationRuleNamedCI>();
            sc.AddSingleton<IValidationRule, ValidationRuleAnyOfTraits>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new RecursiveTrait[] {
            GenericTraitEntityHelper.Class2RecursiveTrait<Validation>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<ValidationIssue>(),
        };

        // TODO: rework to work with quartz
        //public override void RegisterHangfireJobRunners()
        //{
        //    RecurringJob.AddOrUpdate<ValidationEngineRunner>(s => s.Run(null), ValidationEngineRunner.CronExpression);
        //}
    }
}
