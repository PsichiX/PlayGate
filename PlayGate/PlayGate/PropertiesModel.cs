using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate
{
    public class PropertiesModel : ICloneable
    {
        #region Public nested classes.

        public class PropertyJsonSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var p = value as Property;
                var o = new JObject();
                o.Add("name", p.Name);
                o.Add("editor", p.Editor);
                o.Add("value", p.Value);
                o.Add("editorData", p.EditorData);
                o.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var o = JObject.Load(reader);
                var properties = o.Properties().ToList();
                var np = properties.Find(i => i.Name == "name");
                if (np == null)
                    throw new Exception("`name` key cannot be found in object: " + o.ToString());
                var n = (string)np.Value;
                var ep = properties.Find(i => i.Name == "editor");
                if (ep == null)
                    throw new Exception("`type` key cannot be found in object: " + o.ToString());
                var e = (string)ep.Value;
                var p = new Property(n, e);
                var pv = properties.Find(i => i.Name == "value");
                if (pv != null)
                    p.Value = new JValue(pv.Value);
                var edv = properties.Find(i => i.Name == "editorData");
                if (edv != null)
                    p.EditorData = new JValue(edv.Value);
                return p;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(Property).IsAssignableFrom(objectType);
            }
        }

        [JsonConverter(typeof(PropertyJsonSerializer))]
        public class Property : ICloneable, IDisposable
        {
            #region Public static functionalities.

            public static Property Create<T>(string name, string editor, T value = default(T))
            {
                var v = Nullable.GetUnderlyingType(typeof(T)) == null && value == null ? null : JValue.FromObject(value);
                return new Property(name, editor, v);
            }

            public static Property Create<VT, EDV>(string name, string editor, VT value = default(VT), EDV editorData = default(EDV))
            {
                var v = Nullable.GetUnderlyingType(typeof(VT)) == null && value == null ? null : JValue.FromObject(value);
                var ed = JValue.FromObject(editorData);
                return new Property(name, editor, v, ed);
            }

            #endregion



            #region Private data.

            private string m_name;
            private string m_editor;
            private JToken m_value;
            private JToken m_editorData;

            #endregion



            #region Public properties.

            public string Name { get { return m_name; } }
            public string Editor { get { return m_editor; } }
            public JToken Value { get { return m_value; } set { m_value = value == null ? null : JValue.FromObject(value); OnValueChanged(new EventArgs()); } }
            public JToken EditorData { get { return m_editorData; } set { m_editorData = value == null ? null : JValue.FromObject(value); } }

            #endregion



            #region Public events.

            public event EventHandler ValueChanged;

            #endregion



            #region Construction and destruction.

            public Property(string name, string editor, JToken value = null, JToken editorData = null)
            {
                if (String.IsNullOrEmpty(name))
                    throw new ArgumentException("`name` argument cannot be eihter empty or null!");
                if (String.IsNullOrEmpty(editor))
                    throw new ArgumentException("`editor` argument cannot be eihter empty or null!");

                m_name = name;
                m_editor = editor;
                Value = value;
                EditorData = editorData;
            }

            public void Dispose()
            {
                m_name = null;
                m_editor = null;
                m_value = null;
                m_editorData = null;
            }

            #endregion



            #region Public functionalities.

            public T Data<T>()
            {
                try
                {
                    return m_value.ToObject<T>();
                }
                catch { return default(T); }
            }

            public void Data<T>(T v)
            {
                Value = JValue.FromObject(v);
            }

            public T EditorAdditionalData<T>()
            {
                return m_editorData != null ? m_editorData.ToObject<T>() : default(T);
            }

            public void EditorAdditionalData<T>(T v)
            {
                EditorData = JValue.FromObject(v);
            }

            public object Clone()
            {
                Property p = new Property(Name, Editor);
                p.Value = Value;
                p.EditorData = EditorData;
                return p;
            }

            #endregion



            #region Protected functionalities.

            public void OnValueChanged(EventArgs e)
            {
                if (ValueChanged != null)
                    ValueChanged(this, e);
            }

            #endregion
        }

        #endregion



        #region Public properties.

        public List<Property> Properties { get; set; }

        #endregion



        #region Construction and destruction.

        public PropertiesModel(IEnumerable<Property> properties = null)
        {
            Properties = properties == null ? new List<Property>() : properties.ToList();
        }

        #endregion



        #region Public functionalities.

        public object Clone()
        {
            PropertiesModel m = new PropertiesModel();
            foreach (var p in Properties)
                m.Properties.Add(p.Clone() as Property);
            return m;
        }

        #endregion
    }
}
