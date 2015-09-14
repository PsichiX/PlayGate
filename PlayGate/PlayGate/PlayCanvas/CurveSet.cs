using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate.PlayCanvas
{
    [JsonConverter(typeof(CurveSet.CurveSetJsonSerializer))]
    public class CurveSet : ICloneable
    {
        #region Public nested classes.

        public class Curves : List<Curve>
        {
            public Curves()
                : base()
            {
            }

            public Curves(Curve.Keys[] curves)
                : base()
            {
                if (curves != null && curves.Length > 0)
                    foreach (var keys in curves)
                        Add(new Curve(keys));
            }

            public Curves(float[][] curves)
                : base()
            {
                if (curves != null && curves.Length > 0)
                    foreach (var keys in curves)
                        if (keys != null && keys.Length % 2 == 0)
                            Add(new Curve(keys));
            }
        }

        public class CurveSetJsonSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var c = value as CurveSet;
                var o = new JObject();
                o.Add("type", JValue.FromObject(c.Type));
                var cl = new JArray();
                foreach (var ci in c.CurvesList)
                    cl.Add(JValue.FromObject(ci));
                o.Add("curves", cl);
                o.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var c = new CurveSet();
                var o = JObject.Load(reader);
                var cs = o.Property("curves");
                if (cs != null && cs.Value.Type == JTokenType.Array && cs.Value.Count() > 0)
                {
                    var csa = cs.Value;
                    for (int i = 0; i < csa.Count(); ++i)
                        c.CurvesList.Add(csa[i].ToObject<Curve>());
                }
                var tp = o.Property("type");
                if (tp != null)
                    c.Type = tp.Value.ToObject<Curve.CurveType>();
                return c;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(CurveSet).IsAssignableFrom(objectType);
            }
        }

        #endregion



        #region Public properties.

        public Curves CurvesList { get { return m_curves; } }
        public Curve.CurveType Type
        {
            get { return m_type; }
            set
            {
                m_type = value;
                foreach (var c in m_curves)
                    c.Type = Type;
            }
        }

        #endregion



        #region Private data.

        private Curve.CurveType m_type;
        private Curves m_curves;

        #endregion



        #region Construction and destruction.

        public CurveSet(float[][] curves, Curve.CurveType type = Curve.CurveType.SmoothStep)
            : this(new Curves(curves), type)
        {
        }

        public CurveSet(Curve.Keys[] curves, Curve.CurveType type = Curve.CurveType.SmoothStep)
            : this(new Curves(curves), type)
        {
        }

        public CurveSet(Curves curves = null, Curve.CurveType type = Curve.CurveType.SmoothStep)
        {
            m_curves = curves == null ? new Curves() : curves;
            Type = type;
        }

        public CurveSet(CurveSet from)
        {
            if (from == null)
                throw new ArgumentNullException("`from` argument cannot be null!");
            m_curves = new Curves();
            foreach (var c in from.CurvesList)
                if (c != null)
                    m_curves.Add(new Curve(c));
            Type = from.Type;
        }

        #endregion



        #region Public functionalities.

        public ICollection<float> Value(float time, ICollection<float> result = null)
        {
            if (result == null)
                result = new List<float>();
            result.Clear();
            foreach (var c in m_curves)
                result.Add(c.Value(time));
            return result;
        }

        public object Clone()
        {
            return new CurveSet(this);
        }

        #endregion
    }
}
