
namespace Omnikeeper.Base.Entity
{
    public class Statistics
    {
        public readonly long CIs;
        public readonly long ActiveAttributes;
        public readonly long ActiveRelations;
        public readonly long Changesets;
        public readonly long AttributeChanges;
        public readonly long RelationChanges;
        public readonly long Layers;
        public readonly long Traits;
        public readonly long Generators;

        public Statistics(long cis, long activeAttributes, long activeRelations, long changesets, long attributeChanges, long relationChanges, long layers, long traits, long generators)
        {
            this.CIs = cis;
            this.ActiveAttributes = activeAttributes;
            this.ActiveRelations = activeRelations;
            this.Changesets = changesets;
            this.AttributeChanges = attributeChanges;
            this.RelationChanges = relationChanges;
            this.Layers = layers;
            this.Traits = traits;
            this.Generators = generators;
        }

    }
}
