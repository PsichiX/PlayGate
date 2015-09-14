using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate.PlayCanvas
{
    [JsonConverter(typeof(Color.ColorJsonSerializer))]
    public class Color
    {
        #region Public nested classes.
        
        public class ColorJsonSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var c = value as Color;
                var a = JArray.FromObject(c.ToArray());
                a.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var a = JArray.Load(reader);
                return Color.FromArray(a.ToObject<float[]>());
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(Color).IsAssignableFrom(objectType);
            }
        }

        #endregion



        #region Public static functionalities.

        public static Color FromArray(float[] value)
        {
            return value == null ? new Color() : new Color(
                value.Length > 0 ? value[0] : 1.0f,
                value.Length > 1 ? value[1] : 1.0f,
                value.Length > 2 ? value[2] : 1.0f,
                value.Length > 3 ? value[3] : 1.0f
                );
        }

        #endregion



        #region Public properties.

        public float R { get { return m_red; } set { m_red = Math.Max(0.0f, Math.Min(1.0f, value)); } }
        public float G { get { return m_green; } set { m_green = Math.Max(0.0f, Math.Min(1.0f, value)); } }
        public float B { get { return m_blue; } set { m_blue = Math.Max(0.0f, Math.Min(1.0f, value)); } }
        public float A { get { return m_alpha; } set { m_alpha = Math.Max(0.0f, Math.Min(1.0f, value)); } }

        #endregion



        #region Private data.

        private float m_red;
        private float m_green;
        private float m_blue;
        private float m_alpha;

        #endregion



        #region Construction and destruction.

        public Color()
            : this(1.0f, 1.0f, 1.0f)
        {
        }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        #endregion



        #region Public functionalities.

        public float[] ToArray()
        {
            return new float[] {
                m_red,
                m_green,
                m_blue,
                m_alpha
            };
        }

        #endregion
    }
}
