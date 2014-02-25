using System;
using System.Collections.Generic;
//using DataConverter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.IO.MemoryMappedFiles;
using DataConverter;
using Microsoft.Kinect;
using JointType = Microsoft.Kinect.JointType;

namespace KinectDataTransmitter
{
    public class KinectDevice
    {
        //private ColorImageFormat _colorImageFormat = ColorImageFormat.Undefined;
        private byte[] _colorImageData;
        //private DepthImageFormat _depthImageFormat = DepthImageFormat.Undefined;
        private short[] _depthImageData;

        private SkeletonTrackerData _skeletonTracker;
        //private FaceTrackerData _faceTracker;
        private StreamWriter _streamWriter;
        //private KinectSensor _currentSensor;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _currentSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper _coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader _reader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] _bodies = null;

        public bool IsTrackingSkeletons { get; set; }
        public bool IsTrackingHands { get; set; }
        public bool IsTrackingFace { get; set; }
        public bool IsWritingColorStream { get; set; }
        public bool IsWritingDepthStream { get; set; }

        public KinectDevice()
        {
            //_faceTracker = new FaceTrackerData();
            _skeletonTracker = new SkeletonTrackerData();
            _streamWriter = new StreamWriter();

            // for Alpha, one sensor is supported
            _currentSensor = KinectSensor.Default;

            if (_currentSensor != null)
            {
                // get the coordinate mapper
                _coordinateMapper = _currentSensor.CoordinateMapper;

                // open the sensor
                _currentSensor.Open();

                // get the depth (display) extents
                FrameDescription frameDescription = _currentSensor.DepthFrameSource.FrameDescription;

                _bodies = new Body[_currentSensor.BodyFrameSource.BodyCount];

                // open the reader for the body frames
                _reader = _currentSensor.BodyFrameSource.OpenReader();

                if (_reader != null)
                {
                    _reader.FrameArrived += Reader_FrameArrived;
                }

                // set the status text
                Console.WriteLine("d|Found a sensor");
                //this.StatusText = Properties.Resources.InitializingStatusTextFormat;
            }
            else
            {
                // on failure, set the status text
                Console.WriteLine("d|No sensor found");
                //this.StatusText = Properties.Resources.NoSensorStatusText;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            BodyFrameReference frameReference = e.FrameReference;

            try
            {
                BodyFrame frame = frameReference.AcquireFrame();

                if (frame != null)
                {
                    // BodyFrame is IDisposable
                    using (frame)
                    {
                        
                            // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                            // As long as those body objects are not disposed and not set to null in the array,
                            // those body objects will be re-used.
                            frame.GetAndRefreshBodyData(_bodies);

                            foreach (Body body in _bodies)
                            {
                                if (body.IsTracked)
                                {

                                    IReadOnlyDictionary<Microsoft.Kinect.JointType, Joint> joints = body.Joints;

                                    // convert the joint points to depth (display) space
                                    Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();
                                    foreach (JointType jointType in joints.Keys)
                                    {
                                        DepthSpacePoint depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(joints[jointType].Position);
                                        jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                    }

                                }
                            }

                            ProcessData();
                           
                    }
                }
            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
            }
        }

        private void ProcessData(/*int skeletonFrameNumber*/)
        {
            /*
            if (IsTrackingFace)
            {
                _faceTracker.ProcessData(_currentSensor, _colorImageFormat, _colorImageData,
                                         _depthImageFormat, _depthImageData, _skeletons,
                                         skeletonFrameNumber);
            }
             * */

            if (IsTrackingHands)
            {
                
            }

            if (IsTrackingSkeletons)
            {
                _skeletonTracker.ProcessData(_bodies);
            }

            /*
            if (IsWritingColorStream)
            {
                _streamWriter.ProcessColorData(_colorImageData);
            }

            if (IsWritingDepthStream)
            {
                _streamWriter.ProcessDepthData(_depthImageData);
            }
            */
        }

    }
}
