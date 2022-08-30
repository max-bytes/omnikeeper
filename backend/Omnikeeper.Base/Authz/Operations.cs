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
    public abstract record class PreMutationOperationContextForTraitEntities(ITrait trait) : IPreMutationOperationContext;
    public abstract record class PostMutationOperationContextForTraitEntities(ITrait trait) : IPostMutationOperationContext;

    public record class PreUpdateContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostUpdateContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreInsertNewContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostInsertNewContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreDeleteContextForTraitEntities(Guid ciid, ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostDeleteContextForTraitEntities(Guid ciid, ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreUpsertContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostUpsertContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreInsertChangesetDataContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostInsertChangesetDataContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreSetRelationsContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostSetRelationsContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreAddRelationsContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostAddRelationsContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);
    public record class PreRemoveRelationsContextForTraitEntities(ITrait trait) : PreMutationOperationContextForTraitEntities(trait);
    public record class PostRemoveRelationsContextForTraitEntities(ITrait trait) : PostMutationOperationContextForTraitEntities(trait);

}
