using Omnikeeper.Base.Inbound;
using System;
using System.Text.RegularExpressions;

namespace OKPluginOIASharepoint
{
    public struct SharepointExternalListItemID : IExternalID, IEquatable<SharepointExternalListItemID>
    {
        public readonly Guid listID;
        public readonly Guid itemID;

        public SharepointExternalListItemID(Guid listID, Guid itemID)
        {
            this.listID = listID;
            this.itemID = itemID;
        }

        public string SerializeToString()
        {
            return $"{itemID}@{listID}";
        }

        public static SharepointExternalListItemID Deserialize(string input)
        {
            var regexGuid = @"[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}";
            var match = Regex.Match(input, @$"(?<itemID>{regexGuid})@(?<listID>{regexGuid})", RegexOptions.IgnoreCase);
            if (!match.Success)
                return default;
            var itemIDStr = match.Groups["itemID"].Value;
            var listIDStr = match.Groups["listID"].Value;
            if (!Guid.TryParse(itemIDStr, out var itemID))
                return default;
            if (!Guid.TryParse(listIDStr, out var listID))
                return default;
            return new SharepointExternalListItemID(listID, itemID);
        }

        public override string ToString()
        {
            return $"{itemID}@{listID}";
        }

        public override bool Equals(object? other)
        {
            var tmp = (SharepointExternalListItemID?)other;
            if (tmp != null)
                return Equals((SharepointExternalListItemID)tmp);
            return false;
        }
        public bool Equals(SharepointExternalListItemID other) => other.listID.Equals(listID) && other.itemID.Equals(itemID);
        public override int GetHashCode() => HashCode.Combine(listID, itemID);

    }
}
