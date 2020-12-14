
namespace Omnikeeper.Base.Entity.DataOrigin
{
    public enum DataOriginType
    {
        Manual, // TODO: distinguish between actual manual and f.e. REST API based
        InboundIngest,
        InboundOnline,
        ComputeLayer
    }

    public class DataOriginV1
    {
        public DataOriginV1(DataOriginType type)
        {
            Type = type;
        }

        public DataOriginType Type {get;}
    } // TODO: equality/hash/...?
}
