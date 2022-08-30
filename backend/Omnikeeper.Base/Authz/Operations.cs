namespace Omnikeeper.Base.Authz
{
    public enum QueryOperation
    {
        Query, // NOTE: we might need to divide this further in the future
    }

    public enum MutationOperationCIs
    {
        MutateCIs,
        CreateCIs,
        TruncateLayer,
        InsertChangesetData,
    }

    public enum MutationOperationTraitEntities {
        Update,
        InsertNew,
        Delete,
        Upsert,
        InsertChangesetData,
        SetRelations,
        AddRelations,
        RemoveRelations,
    }
}
