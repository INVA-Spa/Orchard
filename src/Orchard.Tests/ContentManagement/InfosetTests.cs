using System.Xml.Linq;
using NUnit.Framework;
using Orchard.ContentManagement.FieldStorage.InfosetStorage;

namespace Orchard.Tests.ContentManagement {
    [TestFixture]
    public class InfosetTests {
        [Test]
        public void DataIsNullWhenNothingWasSet() {
            var infoset = new Infoset();
            Assert.That(infoset.Data, Is.Null);
        }

        [Test]
        public void DataReturnsTheOriginalStringAfterSet() {
            // Spazi e virgolette singole: la riserializzazione produrrebbe una stringa diversa.
            const string original = "<Data><Part  Foo='1' /></Data>";
            var infoset = new Infoset { Data = original };
            Assert.That(infoset.Data, Is.SameAs(original));
        }

        [Test]
        public void DataReflectsAttributeChangesOnRoot() {
            var infoset = new Infoset { Data = "<Data />" };
            infoset.Element.SetAttributeValue("Foo", "1");
            Assert.That(infoset.Data, Is.EqualTo("<Data Foo=\"1\" />"));
        }

        [Test]
        public void DataReflectsChangesDeepInTheTree() {
            var infoset = new Infoset { Data = "<Data><Part><Field Value=\"a\" /></Part></Data>" };
            StringAssert.Contains("Value=\"a\"", infoset.Data);

            infoset.Element.Element("Part").Element("Field").SetAttributeValue("Value", "b");
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part><Field Value=\"b\" /></Part></Data>"));

            infoset.Element.Element("Part").Add(new XElement("Other", "text"));
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part><Field Value=\"b\" /><Other>text</Other></Part></Data>"));

            infoset.Element.Element("Part").Element("Other").FirstNode.ReplaceWith(new XText("changed"));
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part><Field Value=\"b\" /><Other>changed</Other></Part></Data>"));

            infoset.Element.Element("Part").Remove();
            Assert.That(infoset.Data, Is.EqualTo("<Data />"));
        }

        [Test]
        public void DataReflectsTextValueChanges() {
            var infoset = new Infoset { Data = "<Data><Part>old</Part></Data>" };
            infoset.Element.Element("Part").Value = "new";
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part>new</Part></Data>"));
        }

        [Test]
        public void ElementIsCreatedOnDemandAndTracked() {
            var infoset = new Infoset();
            infoset.Element.Add(new XElement("Part", new XAttribute("Foo", "1")));
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part Foo=\"1\" /></Data>"));

            infoset.Element.Element("Part").SetAttributeValue("Foo", "2");
            Assert.That(infoset.Data, Is.EqualTo("<Data><Part Foo=\"2\" /></Data>"));
        }

        [Test]
        public void SettingNullOrEmptyClearsData() {
            var infoset = new Infoset { Data = "<Data Foo=\"1\" />" };
            infoset.Data = null;
            Assert.That(infoset.Data, Is.Null);

            infoset.Data = "<Data Foo=\"1\" />";
            infoset.Data = "";
            Assert.That(infoset.Data, Is.Null);
        }

        [Test]
        public void ChangesToAReplacedElementDoNotAffectData() {
            var infoset = new Infoset { Data = "<Data A=\"1\" />" };
            var oldElement = infoset.Element;

            const string replacement = "<Data B=\"2\" />";
            infoset.Data = replacement;
            oldElement.SetAttributeValue("A", "changed");

            Assert.That(infoset.Data, Is.SameAs(replacement));
        }
    }
}
