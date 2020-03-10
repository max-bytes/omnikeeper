using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class ChangesetType : ObjectGraphType<Changeset>
    {
        public ChangesetType()
        {
            Field(x => x.Timestamp);
            Field(x => x.Username);
            Field("id", x => x.ID);
        }
    }

}
