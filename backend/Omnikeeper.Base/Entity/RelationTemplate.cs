
namespace Omnikeeper.Base.Entity
{
    public class RelationTemplate
    {
        public readonly string PredicateID;
        public readonly bool DirectionForward;

        public readonly string[] TraitHints;

        public RelationTemplate(string predicateID, bool directionForward, string[]? traitHints = null)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            TraitHints = traitHints ?? System.Array.Empty<string>();
        }
    }
}
