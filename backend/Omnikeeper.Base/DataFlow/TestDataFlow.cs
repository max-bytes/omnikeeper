using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Omnikeeper.Base.DataFlow
{
    public class AttributeChange
    {
        public readonly CIAttribute NewAttribute;
        public readonly Guid CIID;
        public readonly string LayerID;
        public readonly bool IsRemoved;

        public AttributeChange(CIAttribute newAttribute, Guid cIID, string layerID, bool isRemoved)
        {
            NewAttribute = newAttribute;
            CIID = cIID;
            LayerID = layerID;
            IsRemoved = isRemoved;
        }
    }

    class TestDataFlow
    {
        public (ITargetBlock<IEnumerable<AttributeChange>> start, IDataflowBlock finish) Define(ILogger logger, IBaseAttributeModel baseAttributeModel, IChangesetProxy changeset, IModelContext trans)
        {
            var a = new TransformManyBlock<IEnumerable<AttributeChange>, AttributeChange>(t => t);

            var tb = new TransformBlock<AttributeChange, AttributeChange>(t =>
            {
                var newValue = t.NewAttribute.Value; // TODO
                var changesetID = Guid.NewGuid(); // TODO
                var attributeID = Guid.NewGuid(); // TODO
                var newAttribute = new CIAttribute(attributeID, "test-gen-attribute", t.CIID, newValue, changesetID);

                return new AttributeChange(newAttribute, t.CIID, t.LayerID, t.IsRemoved);
            });

            a.LinkTo(tb, (c) =>
            {
                return c.LayerID == "tsa_cmdb" && c.NewAttribute.Name == "__name";
            });

            //var ob = new ActionBlock<AttributeChange>(t =>
            //{
            //    if (t.IsRemoved)
            //        logger.LogInformation("Removing calculated attribute!");
            //    else
            //        logger.LogInformation("Calculated new attribute!");
            //});

            var ob = Utils.DefineAttributeCreator(baseAttributeModel, changeset, trans);

            tb.LinkTo(ob);

            //var f = new ActionBlock<bool>(b =>
            //{

            //});

            return (a, ob);
        }
    }

    public class Utils
    {
        public static ITargetBlock<AttributeChange> DefineAttributeCreator(IBaseAttributeModel baseAttributeModel, IChangesetProxy changeset, IModelContext trans)
        {
            return new ActionBlock<AttributeChange>(async t =>
            {
                if (t.IsRemoved)
                {
                    await baseAttributeModel.RemoveAttribute(t.NewAttribute.Name, t.CIID, t.LayerID, changeset, new DataOriginV1(DataOriginType.ComputeLayer), trans);
                }
                else
                {
                    await baseAttributeModel.InsertAttribute(t.NewAttribute.Name, t.NewAttribute.Value, t.CIID, t.LayerID, changeset, new DataOriginV1(DataOriginType.ComputeLayer), trans);
                }
            });
        }

        //public static ITargetBlock<AttributeChange> DefineAttributeGenerator(DataFlowLatestAttributeKeeper attributeKeeper)
        //{
        //    return new ActionBlock<AttributeChange>(t =>
        //    {
        //        attributeKeeper.Update(t);
        //    });
        //}
    }
}
