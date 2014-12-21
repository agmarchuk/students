using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms.DataVisualization.Charting;

namespace PolarProblems
{
    /// <summary>
    /// Логика взаимодействия для Charts.xaml
    /// </summary>
    public partial class Charts : Window
    {
        public Charts()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Все графики находятся в пределах области построения ChartArea, создадим ее
            chart.ChartAreas.Add(new ChartArea("Default"));

            // Добавим линию, и назначим ее в ранее созданную область "Default"
            chart.Series.Add(new Series("Series1"));
            chart.Series["Series1"].ChartArea = "Default";
            chart.Series["Series1"].ChartType = SeriesChartType.Line;

            // добавим данные линии
            //double[] axisXData = new double[] { 1, 2, 3 };//количество записей 
            //double[] axisYData = new double[] { 0.1, 1.5, 1.9 };//скорость = запросы/сек
            //chart.Series["Series1"].Points.DataBindXY(axisXData, axisYData);

            //название осей
            //chart.Series["Series1"].Label = "раздватри";
        }
    }
}
