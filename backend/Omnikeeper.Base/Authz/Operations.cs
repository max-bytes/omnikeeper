using Omnikeeper.Base.Entity;
using System;

namespace Omnikeeper.Base.Authz
{
    // QUERIES
    public interface IQueryOperationContext { }
    public record class QueryOperationContext : IQueryOperationContext { } // NOTE: we might need to divide this further in the future


    // MUTATIONS
    public interface IPreMutationOperationContext { }
    public interface IPostMutationOperationContext { }

    // CIs
    public interface IPreMutationOperationContextForCIs : IPreMutationOperationContext { }
    public interface IPostMutationOperationContextForCIs : IPostMutationOperationContext { }

    public record class PreMutateContextForCIs() : IPreMutationOperationContextForCIs;
    public record class PostMutateContextForCIs() : IPostMutationOperationContextForCIs;
    public record class PreInsertChangesetDataContextForCIs() : IPreMutationOperationContextForCIs;
    public record class PostInsertChangesetDataContextForCIs() : IPostMutationOperationContextForCIs;
    public record class PreCreateContextForCIs() : IPreMutationOperationContextForCIs;
    public record class PostCreateContextForCIs() : IPostMutationOperationContextForCIs;
    public record class PreTruncateLayerContextForCIs() : IPreMutationOperationContextForCIs;
    public record class PostTruncateLayerContextForCIs() : IPostMutationOperationContextForCIs;

    // trait entities
    public abstract record class PreMutationOperationContextForTraitEntities(ITrait Trait) : IPreMutationOperationContext;
    public abstract record class PostMutationOperationContextForTraitEntities(ITrait Trait) : IPostMutationOperationContext;

    public record class PreUpdateContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostUpdateContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreInsertNewContextForTraitEntities(ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostInsertNewContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreDeleteContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostDeleteContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreUpsertContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostUpsertContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreInsertChangesetDataContextForTraitEntities(ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostInsertChangesetDataContextForTraitEntities(ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreSetRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostSetRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreAddRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostAddRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
    public record class PreRemoveRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PreMutationOperationContextForTraitEntities(Trait);
    public record class PostRemoveRelationsContextForTraitEntities(Guid CIID, ITrait Trait) : PostMutationOperationContextForTraitEntities(Trait);
}
