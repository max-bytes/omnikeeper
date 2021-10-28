namespace Omnikeeper.Base.Service
{
    public class UsageTrackingService
    {
        public const string ElementTypeTrait = "trait";

        public void TrackUseTrait(string elementName, string username)
        {
            TrackUse(ElementTypeTrait, elementName, username);
        }

        public void TrackUse(string elementType, string elementName, string username)
        {

        }
    }
}
