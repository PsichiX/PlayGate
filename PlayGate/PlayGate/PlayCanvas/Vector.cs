using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate.PlayCanvas
{
    [JsonConverter(typeof(Vector.VectorJsonSerializer))]
    public class Vector
    {
        #region Public nested classes.

        public class VectorJsonSerializer : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var v = value as Vector;
                var a = JArray.FromObject(v.ToArray());
                a.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var a = JArray.Load(reader);
                return Vector.FromArray(a.ToObject<float[]>());
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(Vector).IsAssignableFrom(objectType);
            }
        }

        #endregion



        #region Public static functionalities.

        public static Vector FromArray(float[] value)
        {
            return value == null ? new Vector() : new Vector(
                value.Length > 0 ? value[0] : 1.0f,
                value.Length > 1 ? value[1] : 1.0f,
                value.Length > 2 ? value[2] : 1.0f
                );
        }

        #endregion



        #region Public properties.

        public float X { get { return m_x; } set { m_x = value; } }
        public float Y { get { return m_y; } set { m_y = value; } }
        public float Z { get { return m_z; } set { m_z = value; } }

        #endregion



        #region Private data.

        private float m_x;
        private float m_y;
        private float m_z;

        #endregion



        #region Construction and destruction.

        public Vector()
            : this(1.0f, 1.0f, 1.0f)
        {
        }

        public Vector(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion



        #region Public functionalities.

        public float[] ToArray()
        {
            return new float[] {
                m_x,
                m_y,
                m_z
            };
        }

        #endregion
    }
}
