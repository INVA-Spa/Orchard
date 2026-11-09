using System.Xml.Linq;

namespace Orchard.ContentManagement.FieldStorage.InfosetStorage {
    public class Infoset {
        private XElement _element;

        // Serializzazione di _element memorizzata: Data viene letto da NHibernate per il
        // dirty checking a ogni (auto)flush, cioè prima di ogni query, per ogni record in
        // sessione. Senza cache ogni lettura riserializzava l'intero XML.
        // Null = da ricalcolare. Viene invalidata dall'evento Changed, che XLinq propaga
        // dall'oggetto modificato a tutti i suoi antenati, quindi copre ogni modifica del sottoalbero.
        private string _data;

        private void SetElement(XElement value, string data) {
            if (_element != null) {
                _element.Changed -= OnElementChanged;
            }
            _element = value;
            if (_element != null) {
                _element.Changed += OnElementChanged;
            }
            _data = data;
        }

        private void OnElementChanged(object sender, XObjectChangeEventArgs e) {
            _data = null;
        }

        public XElement Element {
            get {
                if (_element == null) {
                    SetElement(new XElement("Data"), null);
                }
                return _element;
            }
        }

        public string Data {
            get {
                if (_element == null) {
                    return null;
                }
                return _data ?? (_data = _element.ToString(SaveOptions.DisableFormatting));
            }
            set {
                if (string.IsNullOrEmpty(value)) {
                    SetElement(null, null);
                }
                else {
                    // Si conserva la stringa originale (quella letta dal DB): finché l'XML non
                    // cambia, Data restituisce esattamente il valore persistito e NHibernate non
                    // vede differenze spurie dovute alla riserializzazione.
                    SetElement(XElement.Parse(value, LoadOptions.PreserveWhitespace), value);
                }
            }
        }
    }
}
