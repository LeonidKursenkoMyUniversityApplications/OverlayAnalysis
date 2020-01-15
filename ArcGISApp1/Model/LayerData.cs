using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlayAnalysis.Model
{
    public class LayerData
    {
        public string Name { set; get; }
        public Esri.ArcGISRuntime.Layers.FeatureLayer Layer { set; get; }
    }
}
