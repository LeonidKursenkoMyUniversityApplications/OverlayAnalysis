using System.Threading.Tasks;
using System.Windows.Media;
using OverlayAnalysis.Model;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;

namespace OverlayAnalysis.Controller
{
    public class LayerController
    {
        private async Task<LayerData> OpenLayerAsync(string fullPath)
        {
            var table = await ShapefileTable.OpenAsync(fullPath);
            var layer = new FeatureLayer(table)
            {
                ID = table.Name,
                DisplayName = table.Name,
                FeatureTable = table
            };
            LayerData layerData = new LayerData()
            {
                Layer = layer,
                Name = table.Name
            };
            return layerData;
        }

        public LayerData OpenLayer(string fullPath)
        {
            return (OpenLayerAsync(fullPath)).Result;
        }
        
    }
}
