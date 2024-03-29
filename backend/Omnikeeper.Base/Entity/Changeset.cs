﻿
using Omnikeeper.Base.Entity.DataOrigin;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public record class Changeset
    {
        public Guid ID { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }
        public string LayerID { get; private set; }
        public DataOriginV1 DataOrigin { get; }
        public long UserID { get; private set; }

        public Changeset(Guid id, long userID, string layerID, DataOriginV1 dataOrigin, DateTimeOffset timestamp)
        {
            ID = id;
            UserID = userID;
            LayerID = layerID;
            DataOrigin = dataOrigin;
            Timestamp = timestamp;
        }
    }

    public class ChangesetCIAttributes
    {
        public Guid CIID { get; private set; }
        public IEnumerable<CIAttribute> Attributes { get; private set; }

        public ChangesetCIAttributes(Guid ciid, IEnumerable<CIAttribute> attributes)
        {
            CIID = ciid;
            Attributes = attributes;
        }
    }

    public record class ChangesetCIChanges(Guid CIID, IEnumerable<CIAttributeChange> AttributeChanges);

    public record class CIAttributeChange(CIAttribute? Before, CIAttribute? After)
    {
        public string Name => Before?.Name ?? After?.Name!;
    }
}
