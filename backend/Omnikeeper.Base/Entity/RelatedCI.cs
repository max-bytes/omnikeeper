//using System;

//namespace Omnikeeper.Base.Entity
//{
//    public class CompactRelatedCI
//    {
//        public Guid RelationID { get; private set; }
//        public Guid FromCIID { get; private set; }
//        public Guid ToCIID { get; private set; }
//        public string PredicateID { get; private set; }
//        public Guid ChangesetID { get; private set; }
//        public string LayerID { get => LayerStackIDs[^1]; }
//        public string[] LayerStackIDs { get; private set; }

//        public CompactRelatedCI(Guid relationID, Guid fromCIID, Guid toCIID, Guid changesetID,
//            string predicateID, string[] layerStackIDs)
//        {
//            RelationID = relationID;
//            FromCIID = fromCIID;
//            ToCIID = toCIID;
//            ChangesetID = changesetID;
//            PredicateID = predicateID;
//            LayerStackIDs = layerStackIDs;
//        }
//    }
//}
