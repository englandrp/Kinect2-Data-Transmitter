using DataConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Kinect;
using JointType = Microsoft.Kinect.JointType;

namespace KinectDataTransmitter
{
    /// <summary>
    /// Class that reads skeleton data from the kinect frames and output it for external usage.
    /// </summary>
    public class SkeletonTrackerData : IDisposable
    {
        private bool _disposed;
        private readonly JointData[] _jointData;

        public SkeletonTrackerData()
        {
            const int jointsNumber = (int)DataConverter.JointType.NumberOfJoints;
            _jointData = new JointData[jointsNumber];
            for (int i = 0; i < jointsNumber; i++)
            {
                _jointData[i] = new JointData();
                _jointData[i].JointId = (DataConverter.JointType)i;
            }
        }

        ~SkeletonTrackerData()
        {
            Dispose(false);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                ResetTracking();
                _disposed = true;
            }
        }

        public void ResetTracking()
        {
            // not sure if there is anything to dispose.
        }
        
        public void ProcessData(Body[] skeletons)
        {
            if (skeletons == null)
            {
                return;
            }

            
            // Assume no nearest skeleton and that the nearest skeleton is a long way away.
            Body nearestSkeleton = null;
            var nearestSqrDistance = double.MaxValue;

            // Look through the skeletons.
            foreach (var skeleton in skeletons)
            {
                // Only consider tracked skeletons.
                if (skeleton.IsTracked)
                {
                    // Find the distance squared.
                    var sqrDistance = (skeleton.Joints[JointType.SpineBase].Position.X * skeleton.Joints[JointType.SpineBase].Position.X) +
                                      (skeleton.Joints[JointType.SpineBase].Position.Y*skeleton.Joints[JointType.SpineBase].Position.Y) +
                                      (skeleton.Joints[JointType.SpineBase].Position.Z*skeleton.Joints[JointType.SpineBase].Position.Z);

                    // Is the new distance squared closer than the nearest so far?
                    if (sqrDistance < nearestSqrDistance)
                    {
                        // Use the new values.
                        nearestSqrDistance = sqrDistance;
                        nearestSkeleton = skeleton;
                    }
                }
               
            }
            
            SendSkeletonData(nearestSkeleton);
         
        }

        
        private void SendSkeletonData(Body skeleton)
        {
            if (skeleton == null)
            {
                return;
            }
            //Console.WriteLine("SendSkeletonData");
            for (int i = 0; i < _jointData.Length; i++ )
            {
                _jointData[i].State = DataConverter.TrackingState.NotTracked;
            }
            IReadOnlyDictionary<JointType, Joint> joints = skeleton.Joints;
            IReadOnlyDictionary<JointType, JointOrientation> jointOrientations = skeleton.JointOrientations;

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                //Brush drawBrush = null;
                int jointInt = (int) jointType;

                Microsoft.Kinect.TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == Microsoft.Kinect.TrackingState.Tracked)
                {
                    //Console.WriteLine(jointInt + " " + jointType);
                    _jointData[jointInt].State = (DataConverter.TrackingState) joints[jointType].TrackingState;
                    _jointData[jointInt].PositionX = joints[jointType].Position.X;
                    _jointData[jointInt].PositionY = joints[jointType].Position.Y;
                    _jointData[jointInt].PositionZ = joints[jointType].Position.Z;

                    _jointData[jointInt].QuaternionX = jointOrientations[jointType].Orientation.X;
                    _jointData[jointInt].QuaternionY = jointOrientations[jointType].Orientation.Y;
                    _jointData[jointInt].QuaternionZ = jointOrientations[jointType].Orientation.Z;
                    _jointData[jointInt].QuaternionW = jointOrientations[jointType].Orientation.W;
                }
            }

            Console.WriteLine(Converter.EncodeSkeletonData(_jointData));
            if (skeleton.IsTracked) { 
                Console.WriteLine(Converter.EncodeLeftHandData((int)skeleton.HandLeftState));
                Console.WriteLine(Converter.EncodeRightHandData((int)skeleton.HandRightState));
            }

        }
  
    }
}
