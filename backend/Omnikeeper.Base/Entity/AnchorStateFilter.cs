﻿using System;

namespace Omnikeeper.Base.Entity
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
        public static bool Contains(this AnchorStateFilter stateFilter, AnchorState state)
        {
            return stateFilter switch
            {
                AnchorStateFilter.ActiveOnly => state == AnchorState.Active,
                AnchorStateFilter.ActiveAndDeprecated => state == AnchorState.Active || state == AnchorState.Deprecated,
                AnchorStateFilter.All => true,
                AnchorStateFilter.MarkedForDeletion => state == AnchorState.MarkedForDeletion,
                _ => throw new Exception("Invalid AnchorStateFilter")
            };
        }
    }
}
