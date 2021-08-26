using System;

namespace Omnikeeper.Base.Validation
{
    public class ValidationIssue
    {
        public readonly string ID;
        public readonly string Message;
        public readonly Guid[] AffectedCIs;

        public ValidationIssue(string id, string message, Guid[] affectedCIs)
        {
            this.ID = id;
            this.Message = message;
            this.AffectedCIs = affectedCIs;
        }
    }
}
