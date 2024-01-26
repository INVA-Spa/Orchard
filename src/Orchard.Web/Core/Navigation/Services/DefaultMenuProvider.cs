using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Navigation.Models;
using Orchard.Localization;
using Orchard.UI.Navigation;

namespace Orchard.Core.Navigation.Services {
    public class DefaultMenuProvider : IMenuProvider {
        private readonly IContentManager _contentManager;

        public DefaultMenuProvider(IContentManager contentManager) {
            _contentManager = contentManager;
            _menuPartsMemory = new Dictionary<int, IEnumerable<MenuPart>>();
        }

        // Prevent doing the same query for MenuParts more than once on a same request
        // in case we are building the same menu several times.
        private Dictionary<int, IEnumerable<MenuPart>> _menuPartsMemory;

        public void GetMenu(IContent menu, NavigationBuilder builder) {
            if (!_menuPartsMemory.ContainsKey(menu.Id)) {
                _menuPartsMemory[menu.Id] = _contentManager
                    .Query<MenuPart, MenuPartRecord>()
                    .Where(x => x.MenuId == menu.Id)
                    .List();
            }
            var menuParts = _menuPartsMemory[menu.Id].ToList<MenuPart>();

            //List of hidden items
            var menuPartsHidden = _contentManager
                .Query<MenuPart, MenuPartRecord>()
                .Where(x => x.MenuId == menu.Id && !x.VisibleAtFrontEnd)
                .List();

            //Removing from menuList the items with VisibleAtFrontEnd set to false
            foreach (var itemHidden in menuPartsHidden) {
                //Copy the list
                MenuPart[] menuParts1 = new MenuPart[menuParts.Count];
                menuParts.CopyTo(menuParts1);
                foreach (var item in menuParts1)
                    if (item.MenuPosition.StartsWith(itemHidden.MenuPosition))
                        menuParts.Remove(item);
            }

            //An attempt to optimize the code above but unsuccessful
            //var menuParts = _contentManager
            //    .Query<MenuPart, MenuPartRecord>()
            //    .Where(x => x.MenuId == menu.Id && x.VisibleAtFrontEnd && !_contentManager
            //    .Query<MenuPart, MenuPartRecord>()
            //    .Where(x => x.MenuId == menu.Id && !x.VisibleAtFrontEnd)
            //    .List().Where(w => x.MenuPosition.StartsWith(w.MenuPosition)).Any())
            //    .List().ToList();

            foreach (var menuPart in menuParts) {
                if (menuPart != null ) {
                    var part = menuPart;

                    string culture = null;
                    // fetch the culture of the content menu item, if any
                    var localized = part.As<ILocalizableAspect>();
                    if (localized != null) {
                        culture = localized.Culture;
                    }

                    if (part.Is<MenuItemPart>())
                        builder.Add(new LocalizedString(HttpUtility.HtmlEncode(part.MenuText)), part.MenuPosition, item => item.Url(part.As<MenuItemPart>().Url).Content(part).Culture(culture).Permission(Contents.Permissions.ViewContent));
                    else
                        builder.Add(new LocalizedString(HttpUtility.HtmlEncode(part.MenuText)), part.MenuPosition, item => item.Action(_contentManager.GetItemMetadata(part.ContentItem).DisplayRouteValues).Content(part).Culture(culture).Permission(Contents.Permissions.ViewContent));
                }
            }
        }
    }
}