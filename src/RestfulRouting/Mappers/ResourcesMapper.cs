﻿using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using System.Linq;

namespace RestfulRouting.Mappers
{
    public interface IResourcesMapper<TController> : IResourcesMapperBase where TController : Controller
    {
        void Collection(Action<AdditionalAction> action);
        void Member(Action<AdditionalAction> action);
        void Constrain(string key, object value);
    }

    public class ResourcesMapper<TController> : ResourcesMapperBase<TController>, IResourcesMapper<TController> where TController : Controller
    {
        private Action<ResourcesMapper<TController>> subMapper;
        private Dictionary<string, HttpVerbs[]> collections = new Dictionary<string, HttpVerbs[]>();
        private Dictionary<string, HttpVerbs[]> members = new Dictionary<string, HttpVerbs[]>();

        public ResourcesMapper(Action<ResourcesMapper<TController>> subMapper = null)
        {
            IncludedActions = new Dictionary<string, Func<Route>>
                                  {
                                      {Names.IndexName, () => GenerateNamedRoute(JoinResources(ResourceName), ResourcePath, ControllerName, Names.IndexName, new[] { "GET" })},
                                      {Names.CreateName, () => GenerateRoute(ResourcePath, ControllerName, Names.CreateName, new[] { "POST" })},
                                      {Names.NewName, () => GenerateNamedRoute("new_" + JoinResources(SingularResourceName), ResourcePath + "/" + Names.NewName, ControllerName, Names.NewName, new[] { "GET" })},
                                      {Names.EditName, () => GenerateNamedRoute("edit_" + JoinResources(SingularResourceName), ResourcePath + "/{id}/" + Names.EditName, ControllerName, Names.EditName, new[] { "GET" })},
                                      {Names.ShowName, () => GenerateNamedRoute(JoinResources(SingularResourceName), ResourcePath + "/{id}", ControllerName, Names.ShowName, new[] { "GET" })},
                                      {Names.UpdateName, () => GenerateRoute(ResourcePath + "/{id}", ControllerName, Names.UpdateName, new[] { "PUT" })},
                                      {Names.DestroyName, () => GenerateRoute(ResourcePath + "/{id}", ControllerName, Names.DestroyName, new[] { "DELETE" })}
                                  };
            this.subMapper = subMapper;
        }

        public void Collection(Action<AdditionalAction> action)
        {
            var additionalAction = new AdditionalAction(collections);
            action(additionalAction);
        }

        public void Member(Action<AdditionalAction> action)
        {
            var additionalAction = new AdditionalAction(members);
            action(additionalAction);
        }

        private Route MemberRoute(string action, params HttpVerbs[] methods)
        {
            if (methods.Length == 0)
                methods = new[] { HttpVerbs.Get };

            return GenerateRoute(ResourcePath + "/{id}/" + action, ControllerName, action, methods.Select(x => x.ToString().ToUpperInvariant()).ToArray());
        }

        private Route CollectionRoute(string action, params HttpVerbs[] methods)
        {
            if (methods.Length == 0)
                methods = new[] { HttpVerbs.Get };

            return GenerateRoute(ResourcePath + "/" + action, ControllerName, action, methods.Select(x => x.ToString().ToUpperInvariant()).ToArray());
        }

        public override void RegisterRoutes(RouteCollection routeCollection)
        {
            if (subMapper != null)
            {
                subMapper.Invoke(this);
            }

            var routes = collections.Select(collection => CollectionRoute(collection.Key, collection.Value)).ToList();

            AddIncludedActions(routes);

            routes.AddRange(members.Select(member => MemberRoute(member.Key, member.Value)));

            if (GenerateFormatRoutes)
                AddFormatRoutes(routes);

            foreach (var route in routes)
            {
                ConfigureRoute(route);
                routeCollection.Add(route);
            }

            if (Mappers.Any())
            {
                BasePath = Join(ResourcePath, "{" + SingularResourceName + "Id}");
                var idConstraint = Constraints["id"];
                if (idConstraint != null)
                {
                    Constraints.Remove("id");
                    Constraints.Add(SingularResourceName + "Id", idConstraint);
                }

                AddResourcePath(SingularResourceName);
                RegisterNested(routeCollection, mapper => mapper.SetParentResources(ResourcePaths));
            }
        }
    }
}
