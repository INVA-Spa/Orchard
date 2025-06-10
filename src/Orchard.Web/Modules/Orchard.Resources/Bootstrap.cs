using Orchard.UI.Resources;

namespace Orchard.Resources {
    public class Bootstrap : IResourceManifestProvider {
        public void BuildManifests(ResourceManifestBuilder builder) {
            var manifest = builder.Add();
            manifest.DefineStyle("Bootstrap").SetUrl("bootstrap.min.css", "bootstrap.css").SetVersion("5.3.6").SetCdn("//ajax.aspnetcdn.com/ajax/bootstrap/5.3.6/css/bootstrap.min.css", "//ajax.aspnetcdn.com/ajax/bootstrap/5.3.6/css/bootstrap.css");
            manifest.DefineScript("Bootstrap").SetUrl("bootstrap.bundle.min.js", "bootstrap.bundle.js").SetVersion("5.3.6").SetDependencies("jQuery").SetCdn("//ajax.aspnetcdn.com/ajax/bootstrap/5.3.6/bootstrap.min.js", "//ajax.aspnetcdn.com/ajax/bootstrap/5.3.6/bootstrap.js");
        }
    }
}
