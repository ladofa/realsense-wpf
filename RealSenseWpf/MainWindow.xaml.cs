using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RealSenseCam;
using cv = OpenCvSharp;

namespace RealSenseWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Cam cam = new Cam();

        

        double getDist(cv.Mat depth, int x, int y)
        {
            
            int margin = 10;
            // look 20x20 (margin * 2) area
            var top = Math.Clamp(y - margin, 0, depth.Rows - 1);
            var bottom = Math.Clamp(y + margin, 0, depth.Rows - 1);
            var left = Math.Clamp(x - margin, 0, depth.Cols - 1);
            var right = Math.Clamp(x + margin, 0, depth.Cols - 1);
            if (top == bottom || left == right)
            {
                return 0;
            }

            var crop = depth[top..bottom, left..right];
            var m = crop.Mean();
            return m.Val0;
        }

        public MainWindow()
        {
            InitializeComponent();
            cam.Init();

            Task.Run(() =>
            {
                while (loop)
                {
                    cam.GetFrame(out cv.Mat depth, out cv.Mat bgr);
                    this.depth = depth;
                    cv.Mat depth_vis = cam.DepthToColor(depth);

                    string output = $"{dist}";



                    this.Dispatcher.Invoke(() =>
                    {
                        ColorImage.Source = cv.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(bgr);
                        DepthImage.Source = cv.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(depth_vis);
                        if (dist!= 0) InfoTextBlock.Text = output;
                    });
                }
            });
        }

        double dist = 0;
        cv.Mat depth;

        private void ColorImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Image image = (Image)sender;
            var p = e.GetPosition(image);
            dist = getDist(depth, (int)(p.X / image.ActualWidth * depth.Width), (int)(p.Y / image.ActualHeight * depth.Height));
        }

        bool loop = true;
        private void Window_Closed(object sender, EventArgs e)
        {
            loop = false;
        }
    }
}