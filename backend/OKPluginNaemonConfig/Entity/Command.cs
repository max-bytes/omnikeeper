using Omnikeeper.Base.Entity;

namespace OKPluginNaemonConfig.Entity
{
    [TraitEntity("commmand", TraitOriginType.Core)]
    public class Command : TraitEntity
    {
        // NOTE id here should be TraitEntityID since in original table it is primary key
        [TraitAttribute("id", "naemon_command.id")]
        public readonly long Id;

        [TraitAttribute("name", "naemon_command.name")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitEntityID]
        public readonly string Name;

        [TraitAttribute("exec", "naemon_command.exec")]
        public readonly string Exec;

        [TraitAttribute("desc_arg1", "naemon_command.desc_arg1")]
        public readonly string DescArg1;

        [TraitAttribute("desc_arg2", "naemon_command.desc_arg2")]
        public readonly string DescArg2;

        [TraitAttribute("desc_arg3", "naemon_command.desc_arg3")]
        public readonly string DescArg3;

        [TraitAttribute("desc_arg4", "naemon_command.desc_arg4")]
        public readonly string DescArg4;

        [TraitAttribute("desc_arg5", "naemon_command.desc_arg5")]
        public readonly string DescArg5;

        [TraitAttribute("desc_arg6", "naemon_command.desc_arg6")]
        public readonly string DescArg6;

        [TraitAttribute("desc_arg7", "naemon_command.desc_arg7")]
        public readonly string DescArg7;

        [TraitAttribute("desc_arg8", "naemon_command.desc_arg8")]
        public readonly string DescArg8;

        public Command()
        {
            Name = "";
            Exec = "";
            DescArg1 = "";
            DescArg2 = "";
            DescArg3 = "";
            DescArg4 = "";
            DescArg5 = "";
            DescArg6 = "";
            DescArg7 = "";
            DescArg8 = "";
        }
    }
}
