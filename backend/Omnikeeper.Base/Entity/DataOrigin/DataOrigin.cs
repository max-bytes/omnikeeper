
using ProtoBuf;

namespace Omnikeeper.Base.Entity.DataOrigin
{
    public enum DataOriginType
    {
        Manual, // TODO: distinguish between actual manual and f.e. REST API based
        InboundIngest,
        InboundOnline, // NOTE, TODO: still part of corresponding database-enum, but shouldn't be
        ComputeLayer,
        Generator // NOTE: not part of corresponding database-enum
    }

    [ProtoContract(SkipConstructor = true)]
    public class DataOriginV1
    {
        public DataOriginV1(DataOriginType type)
        {
            Type = type;
        }

        [ProtoMember(1)]
        public readonly DataOriginType Type;
    } // TODO: equality/hash/...?
}
