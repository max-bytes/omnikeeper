using System;

namespace Omnikeeper.Base.Entity.Issue
{
    [TraitEntity("__meta.issue.issue", TraitOriginType.Core)]
    public class Issue : TraitEntity
    {
        [TraitAttribute("id", "okissue.id")]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("type", "okissue.type")]
        [TraitEntityID]
        public readonly string Type;

        [TraitAttribute("context", "okissue.context")]
        [TraitEntityID]
        public readonly string Context;

        [TraitAttribute("message", "okissue.message")]
        public readonly string Message;

        [TraitAttribute("name", "__name", optional: true)]
        public readonly string? Name;

        [TraitRelation("affected_cis", "affects_ci", true)]
        public readonly Guid[] AffectedCIs;


        public Issue()
        {
            ID = "";
            Type = "";
            Context = "";
            Message = "";
            Name = null;
            AffectedCIs = Array.Empty<Guid>();
        }

        public Issue(string iD, string type, string context, string message, Guid[] affectedCIs)
        {
            ID = iD;
            Type = type;
            Context = context;
            Message = message;
            Name = $"OKIssue - {type} - {context} - {iD}";
            AffectedCIs = affectedCIs;
        }
    }
}