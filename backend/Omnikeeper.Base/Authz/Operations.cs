namespace Omnikeeper.Base.Authz
{
    public enum QueryOperation
    {
        Query, // NOTE: we might need to divide this further in the future
    }

    public enum MutationOperation
    {
        MutateCIs,
        CreateCIs,
        TruncateLayer,
        InsertChangesetData,
        TraitEntities_Update,
        TraitEntities_InsertNew,
        TraitEntities_Delete,
        TraitEntities_Upsert,
        TraitEntities_InsertChangesetData,
        TraitEntities_SetRelations,
        TraitEntities_AddRelations,
        TraitEntities_RemoveRelations,
    }
}
