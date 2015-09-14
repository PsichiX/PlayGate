using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate.PlayCanvas
{
    [JsonConverter(typeof(Curve.CurveJsonSerializer))]
    public class Curve : ICloneable
    {
        #region Public enumerations.

        public enum CurveType
        {
            Linear,
            SmoothStep,
            Catmull,
            Cardinal
        }

        #endregion



        #region Public nested classes.

        public class Keys : Dictionary<float, float>
        {
            public Keys()
                : base()
            {
            }

            public Keys(float[] keys)
                : base()
            {
                if (keys != null && keys.Length % 2 == 0)
                    for (var i = 0; i < keys.Length; i += 2)
                        Add(keys[i], keys[i + 1]);
            }
        }

        public class CurveJsonSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var c = value as Curve;
                var o = new JObject();
                o.Add("type", JValue.FromObject(c.Type));
                o.Add("tension", JValue.FromObject(c.Tension));
                var k = new JArray();
                foreach (var kv in c.KeyFrames.OrderBy(kv => kv.Key))
                {
                    k.Add(kv.Key);
                    k.Add(kv.Value);
                }
                o.Add("keys", k);
                o.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var c = new Curve();
                var o = JObject.Load(reader);
                var tp = o.Property("type");
                if (tp != null)
                    c.Type = tp.Value.ToObject<CurveType>();
                var tn = o.Property("tension");
                if (tn != null)
                    c.Tension = tn.Value.ToObject<float>();
                var ks = o.Property("keys");
                if (ks != null && ks.Value.Type == JTokenType.Array && ks.Value.Count() > 0 && ks.Value.Count() % 2 == 0)
                {
                    var ksa = ks.Value;
                    for (int i = 0; i < ksa.Count(); i += 2)
                        c.KeyFrames[ksa[i].ToObject<float>()] = ksa[i + 1].ToObject<float>();
                }
                return c;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(Curve).IsAssignableFrom(objectType);
            }
        }

        #endregion



        #region Public properties.

        public Keys KeyFrames { get { return m_keys; } }
        public CurveType Type { get { return m_type; } set { m_type = value; } }
        public float Tension { get { return m_tension; } set { m_tension = value; } }

        #endregion



        #region Private data.

        private Keys m_keys;
        private CurveType m_type;
        private float m_tension;

        #endregion



        #region Construction and destruction.

        public Curve(float[] keys, CurveType type = CurveType.SmoothStep)
            : this(new Keys(keys), type)
        {
        }

        public Curve(Keys keys = null, CurveType type = CurveType.SmoothStep)
        {
            m_keys = keys == null ? new Keys() : keys;
            m_type = type;
            m_tension = 0.5f;
        }

        public Curve(Curve from)
        {
            if (from == null)
                throw new ArgumentNullException("`from` argument cannot be null!");
            m_keys = new Keys();
            foreach (var kv in from.KeyFrames)
                m_keys.Add(kv.Key, kv.Value);
            m_type = from.Type;
        }

        #endregion



        #region Public functionalities.

        public float Value(float time)
        {
            if (m_keys.Count == 0)
                return 0.0f;

            var ordered = m_keys.OrderBy(kv => kv.Key);

            if (time < ordered.ElementAt(0).Key)
                return ordered.ElementAt(0).Value;
            else if (time > ordered.ElementAt(ordered.Count() - 1).Key)
                return ordered.ElementAt(ordered.Count() - 1).Value;

            float leftTime = 0.0f;
            float leftValue = ordered.ElementAt(0).Value;
            float rightTime = 1.0f;
            float rightValue = 0.0f;
            int index = 0;
            foreach (var frame in ordered)
            {
                if (frame.Key == time)
                    return frame.Value;
                rightValue = frame.Value;
                if (time < frame.Key)
                {
                    rightTime = frame.Key;
                    break;
                }
                leftTime = frame.Key;
                leftValue = frame.Value;
                ++index;
            }
            float timeDiff = rightTime - leftTime;
            float interpolation = (timeDiff == 0.0f ? 0.0f : (time - leftTime) / timeDiff);
            if (m_type == CurveType.SmoothStep)
                interpolation *= interpolation * (3.0f - 2.0f * interpolation);
            else if (m_type == CurveType.Cardinal || m_type == CurveType.Catmull)
            {
                float p1 = leftValue;
                float p2 = rightValue;
                float p0 = p1 + (p1 - p2);
                float p3 = p2 + (p2 - p1);
                float dt1 = rightTime - leftTime;
                float dt0 = dt1;
                float dt2 = dt1;
                if (index > 0)
                    index -= 1;
                if (index > 0)
                {
                    p0 = ordered.ElementAt(index - 1).Value;
                    dt0 = ordered.ElementAt(index).Key - ordered.ElementAt(index - 1).Key;
                }
                if (ordered.Count() > index + 1)
                    dt1 = ordered.ElementAt(index + 1).Key - ordered.ElementAt(index).Key;
                if (ordered.Count() > index + 2)
                {
                    dt2 = ordered.ElementAt(index + 2).Key - ordered.ElementAt(index + 1).Key;
                    p3 = ordered.ElementAt(index + 2).Value;
                }
                p0 = p1 + (p0 - p1) * dt1 / dt0;
                p3 = p2 + (p3 - p2) * dt1 / dt2;
                if (m_type == CurveType.Catmull)
                    return InterpolateCatmullRom(p0, p1, p2, p3, interpolation);
                else
                    return InterpolateCardinal(p0, p1, p2, p3, interpolation, m_tension);
            }
            return leftValue + (rightValue - leftValue) * Math.Max(0.0f, Math.Min(1.0f, interpolation));
        }

        public object Clone()
        {
            return new Curve(this);
        }

        #endregion



        #region Private functionalities.

        private float InterpolateHermite(float p0, float p1, float t0, float t1, float s)
        {
            var s2 = s * s;
            var s3 = s * s * s;
            var h0 = 2 * s3 - 3 * s2 + 1;
            var h1 = -2 * s3 + 3 * s2;
            var h2 = s3 - 2 * s2 + s;
            var h3 = s3 - s2;
            return p0 * h0 + p1 * h1 + t0 * h2 + t1 * h3;
        }

        private float InterpolateCardinal(float p0, float p1, float p2, float p3, float s, float t)
        {
            var t0 = t * (p2 - p0);
            var t1 = t * (p3 - p1);
            return InterpolateHermite(p1, p2, t0, t1, s);
        }

        private float InterpolateCatmullRom(float p0, float p1, float p2, float p3, float s)
        {
            return InterpolateCardinal(p0, p1, p2, p3, s, 0.5f);
        }

        #endregion
    }
}
