﻿using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;

namespace Tasks.Tools
{
    [Explicit]
    class BuildTrait
    {
        [Test]
        public void Build()
        {
            var trait = new RecursiveTrait("host", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    });

            var json = RecursiveTrait.Serializer.SerializeToString(trait);

            Console.WriteLine(json);
        }
    }
}
