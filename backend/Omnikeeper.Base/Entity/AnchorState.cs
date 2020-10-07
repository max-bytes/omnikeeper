namespace Omnikeeper.Base.Entity
{
    public enum AnchorState
    {
        Active,
        Deprecated, // anchor can be set via processes, but not via UIs; UI selection lists show item as deprecated
        Inactive, // anchor cannot be set via processes or UIs, does not show up in UI selection lists
        MarkedForDeletion // anchor cannot be set via processes or UIs, does not show up in UI selection lists, will be deleted when possible
    }
}
