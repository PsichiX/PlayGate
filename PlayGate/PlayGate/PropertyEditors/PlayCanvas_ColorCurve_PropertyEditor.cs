using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayGate.PropertyEditors
{
    public class PlayCanvas_ColorCurve_PropertyEditor : PlayCanvas_Curve_PropertyEditor
    {
        #region Public enumerations.

        public enum ColorCurveType
        {
            [JsonProperty("r")]
            R,
            [JsonProperty("g")]
            G,
            [JsonProperty("b")]
            B,
            [JsonProperty("rgb")]
            RGB,
            [JsonProperty("rgba")]
            RGBA,
        }

        #endregion



        #region Public nested classes.

        public class ColorCurveInfo
        {
            [JsonProperty("type")]
            public ColorCurveType Type { get; set; }
        }

        #endregion



        #region Public static functionalities.

        public new static JToken GetDefaultValue()
        {
            return JValue.FromObject(new float[][] { new float[] { 0.0f, 1.0f }, new float[] { 0.0f, 1.0f }, new float[] { 0.0f, 1.0f }, new float[] { 0.0f, 1.0f } });
        }

        #endregion



        #region Construction and destruction.

        public PlayCanvas_ColorCurve_PropertyEditor(string name, PropertiesModel.Property property)
            : base(name, property)
        {
        }

        #endregion



        #region Public functionalities.

        public override void Initialize(PropertiesModel.Property property)
        {
            ColorCurveInfo colorCurveInfo = property.EditorAdditionalData<ColorCurveInfo>();
            CurveInfo curveInfo = new CurveInfo();
            if (colorCurveInfo.Type == ColorCurveType.R)
                curveInfo.Curves = new string[] { "Red" };
            else if (colorCurveInfo.Type == ColorCurveType.G)
                curveInfo.Curves = new string[] { "Green" };
            if (colorCurveInfo.Type == ColorCurveType.B)
                curveInfo.Curves = new string[] { "Blue" };
            else if (colorCurveInfo.Type == ColorCurveType.RGB)
                curveInfo.Curves = new string[] { "Red", "Green", "Blue" };
            else if (colorCurveInfo.Type == ColorCurveType.RGBA)
                curveInfo.Curves = new string[] { "Red", "Green", "Blue", "Alpha" };
            PropertiesModel.Property p = new PropertiesModel.Property(property.Name, property.Editor, property.Value.DeepClone());
            p.EditorAdditionalData<CurveInfo>(curveInfo);

            base.Initialize(p);
        }

        #endregion
    }
}
