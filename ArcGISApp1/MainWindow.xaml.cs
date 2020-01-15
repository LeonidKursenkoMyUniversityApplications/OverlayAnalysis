using OverlayAnalysis.Model;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OverlayAnalysis.Controller;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Tasks.NetworkAnalyst;

namespace OverlayAnalysis
{
    public partial class MainWindow : Window
    {
        private static readonly string _srcPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string _shpPath = System.IO.Path.Combine(_srcPath, @"data\State_2010Census_DP1.shp");
        // @"C:\Users\Leonid\Documents\ArcGIS\Карты_ArcGIS+методичка\World_Arc\WORLD_region.shp"
        private static readonly string _catalogPath = @"D:\КПІ\4 Курс\ГІС\Карты+зад лабор 4\Карты+зад лабор 4\";
        
        private LayerController _layerController;

        public MainWindow()
        {
            InitializeComponent();
            _layerController = new LayerController();
        }

        private void MyMapView_LayerLoaded(object sender, LayerLoadedEventArgs e)
        {
            if (e.LoadError == null)
                return;

            Debug.WriteLine(string.Format("Error while loading layer : {0} - {1}", e.Layer.ID, e.LoadError.Message));
        }
        private void AddMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Layer";
            dlg.DefaultExt = ".shp"; // Default file extension
            dlg.Filter = "Shape files (.shp)|*.shp"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                AddLayer(filename);
            }
        }

        private void AddLayer(string filename)
        {
            LayerData layerData;
            try
            {
                layerData = _layerController.OpenLayer(filename);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Shape file:\"{filename}\" can`t be read. {e.Message}");
                return;
            }
            
            AddItemToListBox(layerData.Name);
            MyMapView.Map.Layers.Add(layerData.Layer);
        }

        private void AddItemToListBox(string name)
        {
            var checkBox = new CheckBox()
            {
                IsChecked = true,
                Name = name + "CheckBox",
                Content = name
            };
            checkBox.Click += CheckBoxOnClick;
            layerListBox.Items.Add(checkBox);
        }

        private void CheckBoxOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if(!(sender is CheckBox checkBox)) return;
            string id = checkBox.Content.ToString();
            var first = MyMapView.Map.Layers.FirstOrDefault(layer => layer.DisplayName == id);
            if(first == null) return;
            first.IsVisible = checkBox.IsChecked == true;
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            int index = layerListBox.SelectedIndex;
            if(index > -1)
            {
                var chBox = (CheckBox) layerListBox.Items[index];
                string id = chBox.Content.ToString();
                MyMapView.Map.Layers.Remove(id);
                layerListBox.Items.RemoveAt(index);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string name = searchTextBox.Text;
            if (string.IsNullOrEmpty(name) == true) return;
            int index = layerListBox.Items.IndexOf(name);
            if (index > -1)
                layerListBox.SelectedIndex = index;
            else
                layerListBox.SelectedIndex = -1;
        }

        private void BuildButton_Click(object sender, RoutedEventArgs e)
        {
            double r1;
            double r2;
            double r3;
            try
            {
                r1 = Convert.ToDouble(R1TextBox.Text);
                r2 = Convert.ToDouble(R2TextBox.Text);
                r3 = Convert.ToDouble(R3TextBox.Text);
            }
            catch
            {
                return;
            }

            var aBufferGrLayer = CreateBuffer(0, Color.FromRgb(255, 0, 0), r1);
            var bBufferGrLayer = CreateBuffer(1, Color.FromRgb(0, 255, 0), r2);
            var dBufferGrLayer = CreateBuffer(2, Color.FromRgb(0, 0, 255), r3);
            var unionGrLayer = Union(aBufferGrLayer, bBufferGrLayer, Color.FromRgb(255, 255, 0));
            Difference(dBufferGrLayer, unionGrLayer, Color.FromRgb(222, 0, 222));
        }
        
        private GraphicsLayer CreateBuffer(int layerIndex, Color color, double radius)
        {
            if (MyMapView.Map.Layers[layerIndex] is FeatureLayer fLayer)
            {
                var fTable = fLayer.FeatureTable;
                var filter = new QueryFilter {WhereClause = "1=1"};
                var features = fTable.QueryAsync(filter).Result;
                var bufferGrLayer = new GraphicsLayer()
                {
                    ID = fLayer.ID + "Buffer",
                    DisplayName = fLayer.DisplayName + "Buffer"
                };
                MyMapView.Map.Layers.Add(bufferGrLayer);
                AddItemToListBox(bufferGrLayer.DisplayName);
                var geometries = features.Select(feature => feature.Geometry);
                double metersToDegrees = 1d / 111.325 / 1000;
                foreach (var geometry in geometries)
                {
                    Esri.ArcGISRuntime.Geometry.Geometry geometryBuffer =
                        GeometryEngine.Buffer(geometry, radius * metersToDegrees);
                    Symbol bufferSymbol = GetGraphicStyle(color);
                    bufferGrLayer.Graphics.Add(new Graphic(geometryBuffer, bufferSymbol));
                }
                return bufferGrLayer;
            }
            else
            {
                throw new Exception("Fail");
            }
        }

        private GraphicsLayer Union(GraphicsLayer firstGrLayer, GraphicsLayer secondGrLayer, Color color)
        {
            var unionGrLayer = new GraphicsLayer()
            {
                ID = firstGrLayer.ID + secondGrLayer.ID + "Union",
                DisplayName = firstGrLayer.DisplayName + secondGrLayer.DisplayName + "Union"
            };
            MyMapView.Map.Layers.Add(unionGrLayer);
            AddItemToListBox(unionGrLayer.DisplayName);
            var geometries1 = firstGrLayer.Graphics.Select(graphic => graphic.Geometry);
            var geometries2 = secondGrLayer.Graphics.Select(graphic => graphic.Geometry);
            var geometries = geometries1.Concat(geometries2);
            Esri.ArcGISRuntime.Geometry.Geometry unionGeometry = GeometryEngine.Union(geometries);
            Symbol bufferSymbol = GetGraphicStyle(color);
            unionGrLayer.Graphics.Add(new Graphic(unionGeometry, bufferSymbol));
            return unionGrLayer;
        }

        private GraphicsLayer Difference(GraphicsLayer firstGrLayer, GraphicsLayer secondGrLayer, Color color)
        {
            var differenceGrLayer = new GraphicsLayer()
            {
                ID = firstGrLayer.ID + secondGrLayer.ID + "Difference",
                DisplayName = firstGrLayer.DisplayName + secondGrLayer.DisplayName + "Difference"
            };
            MyMapView.Map.Layers.Add(differenceGrLayer);
            AddItemToListBox(differenceGrLayer.DisplayName);
            var geometries1 = firstGrLayer.Graphics.Select(graphic => graphic.Geometry);
            var unionGeometry1 = GeometryEngine.Union(geometries1);
            var geometries2 = secondGrLayer.Graphics.Select(graphic => graphic.Geometry);
            var unionGeometry2 = GeometryEngine.Union(geometries2);
            var differenceGeometry = GeometryEngine.Difference(unionGeometry1, unionGeometry2);
            Symbol bufferSymbol = GetGraphicStyle(color);
            differenceGrLayer.Graphics.Add(new Graphic(differenceGeometry, bufferSymbol));
            return differenceGrLayer;
        }

        private Symbol GetGraphicStyle(Color color)
        {
            return new SimpleFillSymbol
            {
                Color = color,
                Style = SimpleFillStyle.Solid
            };
        }
        
    }
}
