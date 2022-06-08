using System;

namespace Omnikeeper.Base.Entity.Issue
{
    [TraitEntity("__meta.issue.issue", TraitOriginType.Core)]
    public class Issue : TraitEntity
    {
        [TraitAttribute("type", "okissue.type")]
        [TraitEntityID]
        public readonly string Type;

        [TraitAttribute("context", "okissue.context")]
        [TraitEntityID]
        public readonly string Context;

        [TraitAttribute("group", "okissue.group")]
        [TraitEntityID]
        public readonly string Group;

        [TraitAttribute("id", "okissue.id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("message", "okissue.message")]
        public readonly string Message;

        [TraitAttribute("name", "__name", optional: true)]
        public readonly string? Name;

        [TraitRelation("affectedCIs", "affects_ci", true)]
        public readonly Guid[] AffectedCIs;


        public Issue()
        {
            Type = "";
            Context = "";
            Group = "";
            ID = "";
            Message = "";
            Name = null;
            AffectedCIs = Array.Empty<Guid>();
        }

        public Issue(string type, string context, string group, string iD, string message, Guid[] affectedCIs)
        {
            Type = type;
            Context = context;
            Group = group;
            ID = iD;
            Message = message;
            Name = $"OKIssue - {type} - {context} - {iD}";
            AffectedCIs = affectedCIs;
        }
    }
}