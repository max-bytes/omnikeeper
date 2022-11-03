using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Controllers.OData
{
    public class MyODataRoutingApplicationModelProvider : IApplicationModelProvider
    {
        public MyODataRoutingApplicationModelProvider(
            IOptions<ODataOptions> options)
        {
            options.Value.AddRouteComponents("api/odata/{context}", EdmCoreModel.Instance);
        }

        /// <summary>
        /// Gets the order value for determining the order of execution of providers.
        /// </summary>
        public int Order => 90;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            var model = new EdmModel();
            const string prefix = "api/odata/{context}";
            foreach (var controllerModel in context.Result.Controllers)
            {
                if (controllerModel.ControllerName == "TraitEntity")
                {
                    ProcessHandleAll(prefix, model, controllerModel);
                    continue;
                }

                if (controllerModel.ControllerName == "Metadata")
                {
                    ProcessMetadata(prefix, model, controllerModel);
                    continue;
                }
            }
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
        }

        private static void ProcessHandleAll(string prefix, IEdmModel model, ControllerModel controllerModel)
        {
            foreach (var actionModel in controllerModel.Actions)
            {
                if (actionModel.ActionName == "GetNavigation")
                {
                    var path = new ODataPathTemplate(
                        new EntitySetTemplateSegment(),
                        new EntitySetWithKeyTemplateSegment(),
                        new NavigationTemplateSegment());

                    actionModel.AddSelector("get", prefix, model, path);
                }
                else if (actionModel.ActionName == "Get")
                {
                    if (actionModel.Parameters.Count == 2)
                    {
                        var path = new ODataPathTemplate(
                            new EntitySetTemplateSegment());
                        actionModel.AddSelector("get", prefix, model, path);
                    }
                    else
                    {
                        var path = new ODataPathTemplate(
                            new EntitySetTemplateSegment(), 
                            new EntitySetWithKeyTemplateSegment());
                        actionModel.AddSelector("get", prefix, model, path);
                    }
                }
            }
        }

        private static void ProcessMetadata(string prefix, IEdmModel model, ControllerModel controllerModel)
        {
            foreach (var actionModel in controllerModel.Actions)
            {
                if (actionModel.ActionName == "GetMetadata")
                {
                    var path = new ODataPathTemplate(
                        MetadataSegmentTemplate.Instance);
                    actionModel.AddSelector("get", prefix, model, path);
                }
                else if (actionModel.ActionName == "GetServiceDocument")
                {
                    var path = new ODataPathTemplate(
                        );
                    actionModel.AddSelector("get", prefix, model, path);
                }
            }
        }

        public class EntitySetTemplateSegment : ODataSegmentTemplate
        {
            public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
            {
                yield return "/{entityset}";
            }

            public override bool TryTranslate(ODataTemplateTranslateContext context)
            {
                if (!context.RouteValues.TryGetValue("entityset", out object classname))
                {
                    return false;
                }

                string entitySetName = classname as string;

                // if you want to support case-insensitive
                var edmEntitySet = context.Model.EntityContainer.EntitySets()
                    .FirstOrDefault(e => string.Equals(entitySetName, e.Name, StringComparison.OrdinalIgnoreCase));

                //var edmEntitySet = context.Model.EntityContainer.FindEntitySet(entitySetName);
                if (edmEntitySet != null)
                {
                    var segment = new EntitySetSegment(edmEntitySet);
                    context.Segments.Add(segment);
                    return true;
                }

                return false;
            }
        }

        public class EntitySetWithKeyTemplateSegment : ODataSegmentTemplate
        {
            public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
            {
                yield return "({key})";
                yield return "/{key}";
            }

            public override bool TryTranslate(ODataTemplateTranslateContext context)
            {
                if (!context.RouteValues.TryGetValue("entityset", out object entitysetNameObj))
                {
                    return false;
                }

                if (!context.RouteValues.TryGetValue("key", out object keyObj))
                {
                    return false;
                }

                string entitySetName = entitysetNameObj as string;
                string keyValue = keyObj as string;

                // if you want to support case-insensitive
                var edmEntitySet = context.Model.EntityContainer.EntitySets()
                    .FirstOrDefault(e => string.Equals(entitySetName, e.Name, StringComparison.OrdinalIgnoreCase));

                //var edmEntitySet = context.Model.EntityContainer.FindEntitySet(entitySetName);
                if (edmEntitySet != null)
                {
                    var entitySet = new EntitySetSegment(edmEntitySet);
                    IEdmEntityType entityType = entitySet.EntitySet.EntityType();

                    IEdmProperty keyProperty = entityType.Key().First();

                    // NOTE: normally we would use
                    // newValue = ODataUriUtils.ConvertFromUriLiteral(keyValue, ODataVersion.V4, context.Model, keyProperty.Type);
                    // but that throws exceptions on some guid values for some unknown reason
                    object newValue = Guid.Parse(keyValue);

                    // for non FromODataUri, so update it, for example, remove the single quote for string value.
                    context.UpdatedValues["key"] = newValue;

                    // For FromODataUri, let's refactor it later.
                    string prefixName = ODataParameterValue.ParameterValuePrefix + "key";
                    context.UpdatedValues[prefixName] = new ODataParameterValue(newValue, keyProperty.Type);

                    IDictionary<string, object> keysValues = new Dictionary<string, object>
                    {
                        [keyProperty.Name] = newValue
                    };

                    var keySegment = new KeySegment(keysValues, entityType, entitySet.EntitySet);

                    context.Segments.Add(entitySet);
                    context.Segments.Add(keySegment);

                    return true;
                }

                return false;
            }
        }
    }

    public class NavigationTemplateSegment : ODataSegmentTemplate
    {
        public override IEnumerable<string> GetTemplates(ODataRouteOptions options)
        {
            yield return "/{navigation}";
        }

        public override bool TryTranslate(ODataTemplateTranslateContext context)
        {
            if (!context.RouteValues.TryGetValue("navigation", out object navigationNameObj))
            {
                return false;
            }

            string navigationName = navigationNameObj as string;
            KeySegment keySegment = context.Segments.Last() as KeySegment;
            IEdmEntityType entityType = keySegment.EdmType as IEdmEntityType;

            IEdmNavigationProperty navigationProperty = entityType.NavigationProperties().FirstOrDefault(n => n.Name == navigationName);
            if (navigationProperty != null)
            {
                var navigationSource = keySegment.NavigationSource;

                var asContained = false;

                if (asContained)
                {
                    var seg = new NavigationPropertySegment(navigationProperty, navigationSource);
                    context.Segments.Add(seg);
                    return true;
                } 
                else
                {
                    IEdmNavigationSource targetNavigationSource = navigationSource.FindNavigationTarget(navigationProperty);
                    var targetType = targetNavigationSource.Type;

                    // if you want to support case-insensitive
                    var edmEntitySet = context.Model.EntityContainer.EntitySets()
                        .FirstOrDefault(e => string.Equals(targetType.AsElementType().FullTypeName(), e.Type.AsElementType().FullTypeName(), StringComparison.OrdinalIgnoreCase));

                    if (edmEntitySet != null)
                    {
                        var segment = new EntitySetSegment(edmEntitySet);
                        context.Segments.Add(segment);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
