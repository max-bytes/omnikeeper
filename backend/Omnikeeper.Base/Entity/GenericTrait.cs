﻿using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public interface ITrait
    {
        public string Name { get; }// TODO: rename to "id" because name is misleading
        public IImmutableSet<string> AncestorTraits { get; }
        public TraitOriginV1 Origin { get; }
    }

    /// <summary>
    /// comparable to RecursiveTrait, but with the recursive dependencies resolved and flattened
    /// </summary>
    public class GenericTrait : ITrait
    {
        private GenericTrait(string name, TraitOriginV1 origin, IImmutableList<TraitAttribute> requiredAttributes, IImmutableList<TraitAttribute> optionalAttributes, ImmutableList<TraitRelation> requiredRelations, IImmutableSet<string> ancestorTraits)
        {
            Name = name;
            Origin = origin;
            RequiredAttributes = requiredAttributes;
            OptionalAttributes = optionalAttributes;
            RequiredRelations = requiredRelations;
            AncestorTraits = ancestorTraits;
        }

        public string Name { get; set; } // TODO: rename to "id" because name is misleading
        public TraitOriginV1 Origin { get; set; }

        public IImmutableSet<string> AncestorTraits { get; set; }
        public IImmutableList<TraitAttribute> RequiredAttributes { get; set; }
        public IImmutableList<TraitAttribute> OptionalAttributes { get; set; }
        public ImmutableList<TraitRelation> RequiredRelations { get; set; }
        // TODO: implement optional relations

        public static GenericTrait Build(string name, TraitOriginV1 origin,
            IEnumerable<TraitAttribute> requiredAttributes,
            IEnumerable<TraitAttribute> optionalAttributes,
            IEnumerable<TraitRelation> requiredRelations,
            ISet<string> ancestorTraits)
        {
            return new GenericTrait(name, origin, requiredAttributes.ToImmutableList(), optionalAttributes.ToImmutableList(), requiredRelations.ToImmutableList(), ancestorTraits.ToImmutableHashSet());
        }
    }
}
