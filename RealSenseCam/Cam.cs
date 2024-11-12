using System;
using Intel.RealSense;
using OpenCvSharp;

namespace RealSenseCam
{
    public class Cam
    {
        public int DEPTH_FRAME_WIDTH = 640;
        public int DEPTH_FRAME_HEIGHT = 480;

        public int RGB_FRAME_WIDTH = 1280;
        public int RGB_FRAME_HEIGHT = 720;

        Pipeline pipe;
        Align align;
        double depthScale;
        public void Init()
        {
            //https://community.intel.com/t5/Items-with-no-label/Depth-post-processing-in-%D1%81/td-p/645136?profile.language=en
            //https://github.com/IntelRealSense/librealsense/issues/8226

            var ctx = new Context();
            var devices = ctx.QueryDevices();
            var device = devices[0].As<SerializableDevice>();

            //json 설정을 하고 싶으면 아래 주석을 해제
            //var device = Intel.RealSense.SerializableDevice.FromDevice(devices[0]);
            //device.JsonConfiguration = System.IO.File.ReadAllText("asdf.json");

            pipe = new Pipeline();
            var config = new Config();
            config.EnableStream(Stream.Depth, DEPTH_FRAME_WIDTH, DEPTH_FRAME_HEIGHT, Format.Z16, 30);
            config.EnableStream(Stream.Color, RGB_FRAME_WIDTH, RGB_FRAME_HEIGHT, Format.Bgr8, 30);
            
            var cfg = pipe.Start(config);
            depthScale = device.Sensors[0].DepthScale;
            depthScale *= 10000;//meter to dmm

            var profile = cfg.GetStream<VideoStreamProfile>(Stream.Color);
            align = new Align(Stream.Color);
        }

        //depth의 단위는 dmm
        public void GetFrame(out Mat depth, out Mat bgr)
        {
            if (pipe == null)
            {
                depth = new Mat();
                bgr = new Mat();
            }
            var frames = pipe.WaitForFrames();
            //align the frame
            frames = align.Process<FrameSet>(frames);

            var colorFrame = frames.ColorFrame.DisposeWith(frames);
            var depthFrame = frames.DepthFrame.DisposeWith(frames);

            //// If we only received new depth frame, 
            //// but the color did not update, continue
            //ulong lastFrameNumber = 0;
            //if (colorFrame.Number == lastFrameNumber) continue;
            //lastFrameNumber = colorFrame.Number;

            bgr = FrameToMat(colorFrame);
            depth = DepthFrameToDeciMilli(ref depthFrame);
        }

        //Function based off of frame_to_mat https://github.com/IntelRealSense/librealsense/blob/master/wrappers/opencv/cv-helpers.hpp and converted to C#
        private OpenCvSharp.Mat FrameToMat(Intel.RealSense.Frame f)
        {
            var vf = f as VideoFrame;
            int w = vf.Width;
            int h = vf.Height;
            Mat? m = null;
            if (vf.Profile.Format == Format.Bgr8)
            {
                
                m = Mat.FromPixelData(h, w, MatType.CV_8UC3, f.Data);
                return m;
            }
            else if (vf.Profile.Format == Format.Rgb8)
            {
                m = Mat.FromPixelData(h, w, MatType.CV_8UC3, f.Data);
                m.CvtColor(ColorConversionCodes.RGB2BGR);
                return m;
            }
            else if (vf.Profile.Format == Format.Z16)
            {
                m = Mat.FromPixelData(h, w, MatType.CV_16UC1, f.Data);
                return m;
            }
            else if (vf.Profile.Format == Format.Y8)
            {
                m = Mat.FromPixelData(h, w, MatType.CV_8UC1, f.Data);
                return m;
            }
            else if (vf.Profile.Format == Format.Disparity32)
            {
                m = Mat.FromPixelData(h, w, MatType.CV_32FC1, f.Data);
                return m;
            }
            else
            {
                //MessageBox.Show("Error occurred!");
                return new Mat();
            }
        }

        //Function to convert depth frame to a matrix of doubles with distances in meters. 
        //Function based off of depth_frame_to_meters https://github.com/IntelRealSense/librealsense/blob/master/wrappers/opencv/cv-helpers.hpp and converted to C#
        private Mat DepthFrameToDeciMilli(ref DepthFrame df)
        {
            Intel.RealSense.Frame f = df as Intel.RealSense.Frame;
            Mat dm = FrameToMat(f);
            dm = dm * depthScale;
            return dm;
        }

        public Mat DepthToColor(Mat depth)
        {
            Mat gray = new Mat(depth.Size(), MatType.CV_8UC1);
            Mat vis = new Mat(depth.Size(), MatType.CV_8UC3);
            Cv2.ConvertScaleAbs(depth, gray, 0.8 / 10000.0 * 255.0); // maxDist / dmm * 255
            OpenCvSharp.Cv2.ApplyColorMap(gray, vis, ColormapTypes.Jet);
            return vis;
        }
    }
}
