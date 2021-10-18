﻿using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Omnikeeper.GridView.Entity
{
    [TraitEntity("__meta.config.gridview_context", TraitOriginType.Core)]
    public class GridViewContext : TraitEntity
    {
        [TraitAttribute("id", "gridview_context.id")]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        [TraitAttributeValueConstraintTextRegex(IDValidations.GridViewContextIDRegexString, IDValidations.GridViewContextIDRegexOptions)]
        [TraitEntityID]
        public readonly string ID;

        [TraitAttribute("speaking_name", "gridview_context.speaking_name", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string SpeakingName;

        [TraitAttribute("description", "gridview_context.description", optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Description;

        [TraitAttribute("name", ICIModel.NameAttribute, optional: true)]
        [TraitAttributeValueConstraintTextLength(1, -1)]
        public readonly string Name;

        [TraitAttribute("config", "gridview_context.config", isJSONSerialized: true)]
        public readonly GridViewConfiguration Configuration;

        public GridViewContext(string id, string speakingName, string description, GridViewConfiguration configuration)
        {
            ID = id;
            SpeakingName = speakingName;
            Description = description;
            Configuration = configuration;
            Name = $"Gridview-Context {ID}";
        }

        public GridViewContext() {
            ID = "";
            SpeakingName = "";
            Description = "";
            Configuration = new GridViewConfiguration(false, "", new List<string>(), new List<GridViewColumn>(), "");
            Name = "";
        }
    }
}