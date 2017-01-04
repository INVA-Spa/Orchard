﻿using Orchard.Mvc.Routes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Laser.Orchard.CommunicationGateway {
    public class Routes : IRouteProvider {
        public void GetRoutes(ICollection<RouteDescriptor> routes) {
            foreach (var routeDescriptor in GetRoutes()) {
                routes.Add(routeDescriptor);
            }
        }

        public IEnumerable<RouteDescriptor> GetRoutes() {
            return new[] {
                AddRoute("CommunicationGatewayAPI/Get/{id}", "DeliveryReport", "Default"),
                AddRoute("CommunicationGatewayAPI/GetByExternalId/{id}", "DeliveryReport", "ExternalId")
            };
        }

        private RouteDescriptor AddRoute(string routePattern, string controllerName, string action) {
            return new RouteDescriptor {
                Priority = 15,
                Route = new Route(
                    routePattern,
                    new RouteValueDictionary {
                            {"area", "Laser.Orchard.CommunicationGateway"},
                            {"controller", controllerName},
                            {"action", action}
                        },
                    new RouteValueDictionary(),
                    new RouteValueDictionary {
                            {"area", "Laser.Orchard.CommunicationGateway"}
                        },
                    new MvcRouteHandler())
            };

        }
    }
}