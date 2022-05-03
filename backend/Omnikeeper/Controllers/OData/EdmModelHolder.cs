using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Controllers.OData
{
    public class EdmModelHolder
    {
        private IEdmModel? model;
        private DateTimeOffset? latestModelCreation;

        private readonly object _lock = new();

        public IEdmModel GetModel()
        {
            lock (_lock)
            {
                if (model == null)
                {
                    throw new Exception("Expected Edm model to be initialized before use");
                }
                return model;
            }
        }


        public DateTimeOffset? GetLatestModelCreation() => latestModelCreation;

        public void ReInitModel(IServiceProvider sp, IDictionary<string, ITrait> activeTraits, ILogger logger)
        {
            logger.LogInformation("(Re-)initializing Edm model...");

            lock (_lock)
            {
                try
                {
                    model = CreateEdmModel(activeTraits, logger);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Encountered error while creating trait entity Edm model");
                }

                latestModelCreation = DateTimeOffset.Now;
            }

            logger.LogInformation("Finished initializing Edm model");
        }


        private IEdmModel CreateEdmModel(IDictionary<string, ITrait> activeTraits, ILogger logger)
        {
            EdmModel model = new EdmModel();

            EdmEntityContainer container = new EdmEntityContainer("ns", "container");

            // attributes
            var edmEntityTypeMap = new Dictionary<string, EdmEntityType>();
            var edmEntitySetMap = new Dictionary<string, EdmEntitySet>();
            foreach (var at in activeTraits)
            {
                if (at.Key == TraitEmpty.StaticID) // ignore the empty trait
                    continue;

                try
                {
                    var typeName = at.Value.ID;
                    var typeNamePlural = typeName + "s";
                    EdmEntityType eet = new EdmEntityType("ns", typeName);
                    eet.AddKeys(eet.AddStructuralProperty("ciid", EdmPrimitiveTypeKind.Guid, false));

                    foreach (var ta in at.Value.RequiredAttributes)
                    {
                        try
                        {
                            var type = ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                            var edmType = EdmModelHelper.AttributeValueType2EdmPrimitiveType(type); // TODO: catch exception

                            if (!ta.AttributeTemplate.IsArray.GetValueOrDefault(false))
                            {
                                eet.AddStructuralProperty(ta.Identifier, edmType, false);
                            }
                            else
                            { // TODO: add support for arrays
                                //eet.AddUnidirectionalNavigation()
                                //var navProp = eet.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
                                //{
                                //    Name = ta.Identifier,
                                //    TargetMultiplicity = EdmMultiplicity.Many,
                                //    Target = edmType,
                                //    ContainsTarget = true,
                                //});
                                //edmEntitySetMap[at.Key].AddNavigationTarget(navProp, edmEntitySetMap[targetTraitID]);
                            }
                        } 
                        catch (Exception)
                        {
                            logger.LogWarning($"Could not create property {ta.Identifier} for edm type {typeName}");
                        }
                    }

                    foreach (var ta in at.Value.OptionalAttributes)
                    {
                        try
                        {
                            if (!ta.AttributeTemplate.IsArray.GetValueOrDefault(false))
                            { // TODO: add support for arrays
                                var type = ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text);
                                eet.AddStructuralProperty(ta.Identifier, EdmModelHelper.AttributeValueType2EdmPrimitiveType(type), true); // TODO: catch exception
                            }
                        }
                        catch (Exception)
                        {
                            logger.LogWarning($"Could not create property {ta.Identifier} for edm type {typeName}");
                        }
                    }

                    model.AddElement(eet);

                    edmEntityTypeMap.Add(at.Key, eet);

                    var entitySet = container.AddEntitySet(typeNamePlural, eet);
                    edmEntitySetMap.Add(at.Key, entitySet);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not create EDM model types for trait entity with trait ID {at.Key}");
                }
            }

            // relations
            foreach (var at in activeTraits)
            {
                foreach (var tr in at.Value.OptionalRelations)
                {
                    try
                    {
                        var baseEdmEntity = edmEntityTypeMap[at.Key];
                        foreach (var targetTraitID in tr.RelationTemplate.TraitHints)
                        {
                            var targetEdmEntity = edmEntityTypeMap[targetTraitID];

                            var navProp = baseEdmEntity.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
                            {
                                Name = tr.Identifier + "_as_" + targetTraitID,
                                TargetMultiplicity = EdmMultiplicity.Many,
                                Target = targetEdmEntity,
                                ContainsTarget = false,
                            });
                            edmEntitySetMap[at.Key].AddNavigationTarget(navProp, edmEntitySetMap[targetTraitID]);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not create edm relation");
                    }
                }
            }

            model.AddElement(container);

            return model;
        }

    }
}
