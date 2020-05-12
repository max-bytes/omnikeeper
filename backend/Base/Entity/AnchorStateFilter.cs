using System;

namespace Landscape.Base.Entity
{
    public enum AnchorStateFilter
    {
        ActiveOnly,
        ActiveAndDeprecated,
        All,
        MarkedForDeletion
    }

    public static class AnchorStateFilterService
    {
        public static AnchorState[] Filter2States(this AnchorStateFilter stateFilter)
        {
            var states = stateFilter switch
            {
                AnchorStateFilter.ActiveOnly => new AnchorState[] { AnchorState.Active },
                AnchorStateFilter.ActiveAndDeprecated => new AnchorState[] { AnchorState.Active, AnchorState.Deprecated },
                AnchorStateFilter.All => (AnchorState[])Enum.GetValues(typeof(AnchorState)),
                AnchorStateFilter.MarkedForDeletion => new AnchorState[] { AnchorState.MarkedForDeletion },
                _ => throw new Exception("Invalid AnchorStateFilter")
            };
            return states;
        }
    }
}
