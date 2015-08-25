using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PolarProblems
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int Npotok=1000;//количество записей в БД
        public Charts ch = new Charts();//форма с графиком

        public MainWindow()
        {
            InitializeComponent();
        }

        //Решение PolarDB
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            WorkMain wm = new WorkMain();

            wm.InitPolar();

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            bool rez = wm.GoLoadPolar(Npotok);
            sw.Stop();
            if (rez == true) Label1.Content = sw.ElapsedMilliseconds;

            sw.Restart();
            rez = wm.GoSearchPolar_name((string)SearchString.Content);

            DateTime dt = new DateTime(2000,10,12);
            //rez = wm.GoSearchPolar_birth(dt.ToBinary());

            sw.Stop();

            if (rez == true) Label5.Content = sw.ElapsedMilliseconds;
            else Label5.Content = "fail";
        }

        //Решение MySQL
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            WorkMain wm = new WorkMain();

            wm.InitMySql();//подготовили БД; удалили данные, если они были
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            bool rez = wm.GoLoadMySql(Npotok);

            sw.Stop();

            if (rez == true) Label2.Content = sw.ElapsedMilliseconds;
            else Label2.Content = "fail";

            sw.Restart();
            rez = wm.GoSearchMySql((string)SearchString.Content);

            sw.Stop();

            if (rez == true) Label6.Content = sw.ElapsedMilliseconds;
            else Label6.Content = "fail";
        }

        //Решение SQLite
        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            WorkMain wm = new WorkMain();

            wm.InitSQLite();//подготовили БД; удалили данные, если они были

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            bool rez = wm.GoLoadSQLite(Npotok);

            sw.Stop();

            if (rez == true) Label4.Content = sw.ElapsedMilliseconds;
            else Label4.Content = "fail";

            sw.Restart();
            rez = wm.GoSearchSQLite((string)SearchString.Content);

            sw.Stop();

            if (rez == true) Label8.Content = sw.ElapsedMilliseconds;
            else Label8.Content = "fail";
        }

        //Решение MS SQL
        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            WorkMain wm = new WorkMain();

            wm.InitMSSQL();//подготовили БД; удалили данные, если они были

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            bool rez = wm.GoLoadMSSQL(Npotok);

            sw.Stop();

            if (rez == true) Label3.Content = sw.ElapsedMilliseconds;
            else Label3.Content = "fail";

            sw.Restart();
            rez = wm.GoSearchMSSQL((string)SearchString.Content);

            sw.Stop();

            if (rez == true) Label7.Content = sw.ElapsedMilliseconds;
            else Label7.Content = "fail";

        }


        private void Problem1_Click(object sender, RoutedEventArgs e)
        {
            Button1_Click(sender, e);
            Button2_Click(sender, e);
            Button3_Click(sender, e);
            Button4_Click(sender, e);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ((Slider)sender).SelectionEnd = e.NewValue;
            Npotok = (int)((Slider)sender).Value;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            WorkMain wm = new WorkMain();
            wm.FinalExit();
        }

        //добавляем наборы записей в БД, замеряем скорость и строим график       
        public void LoadElementFlow()
        {
            WorkMain wm = new WorkMain();
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            //int[][] xy = new int [2][];
            //for (int i = 0; i <2; i++)
            //    xy[i] = new int[11];

            int maxDataNum = 100000;
            int minDataNum = 5000;
            int[] x = new int[maxDataNum / minDataNum +1];
            double speed = 0;

            //Тестируем БД на наборах записей
            for (int i = minDataNum; i <= maxDataNum; i = i + minDataNum)
            {
               //wm.InitPolar();
                wm.InitSQLite();

                sw.Restart();
                    //wm.GoLoadPolar(i);
                    //wm.GoLoadMSSQL(i);
                    //wm.GoLoadMySql(i);
                    wm.GoLoadSQLite(i);
                sw.Stop();
                
                long time = sw.ElapsedMilliseconds;//время добавления записей в БД
                //speed = (double)i / ((double)time / 1000.0);
                //xy[0][i / minDataNum] = (int)(speed);
                //x[i / minDataNum] = (int)(speed);
 
                sw.Restart();
                    //wm.GoSearchPolar_name((string)SearchString.Content);
                    //wm.GoSearchMSSQL((string)SearchString.Content);
                    //wm.GoSearchMySql((string)SearchString.Content);
                    wm.GoSearchSQLite((string)SearchString.Content);
                sw.Stop();

                time = sw.ElapsedMilliseconds;//время поиска всех записей в БД, в которых содержится искомая строка
                speed = (double)i / ((double)time / 1000.0);
                //xy[1][i / minDataNum] = (int)(speed);
                x[i / minDataNum] = (int)(speed); 

                //ch.chart.Series["Series1"].Points.AddXY(i, (double)i / (double)time * 100);
                wm.FinalExit();
            }

//            wm.Draw2(x);//построение графика в Excel
        }

        private void ButtonCharts_Click(object sender, RoutedEventArgs e)
        {
            //this.ch.Show();//показ графика
            LoadElementFlow();
        }

                
    }
}
