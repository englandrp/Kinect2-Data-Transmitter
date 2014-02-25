using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DataConverter
{
    /// <summary>
    /// Class responsible for converting data from the file mapping data stream to managed data that can be used in code.
    /// </summary>
    public static class Converter
    {
        // chars encoding the types of messages that can be transmitted.
        public const char ErrorType = 'E';
        public const char PingType = 'P';
        public const char FaceTrackingFrameType = 'F';
        public const char VideoFrameType = 'V';
        public const char DepthFrameType = 'D';
        public const char DebugType = 'd';
        public const char SkeletonFrameType = 'S';
        public const char HandLeftType = 'L';
        public const char HandRightType = 'R';
        /// <summary>
        /// Maximum number of joints in a skeleton.
        /// </summary>
        public static int JointCount = Enum.GetValues(typeof(JointType)).Length;

        /// <summary>
        /// The name of the file used to transfer color data.
        /// </summary>
        private const string ColorFileName = "KinectColorFrame";
        /// <summary>
        /// The name of the file used to transfer depth data.
        /// </summary>
        private const string DepthFileName = "DepthColorFrame";
        /// <summary>
        /// The size of the file used to transfer color data.
        /// </summary>
        private const long ColorFileSize = 640 * 480 * 4;
        /// <summary>
        /// The size of the file used to transfer depth data.
        /// </summary>
        private const long DepthFileSize = 640 * 480 * 2;
        /// <summary>
        /// The code corresponding to "read" access level.
        /// </summary>
        private const int ReadAccess = 4;

        private static StringBuilder _stringBuilder = new StringBuilder();

        /// <summary>
        /// Opens a named file mapping object.
        /// </summary>
        /// <param name="desiredAccess">The access to the file mapping object. This access is checked against any security descriptor on the target file mapping object. 
        /// For a list of values, see <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/aa366559(v=vs.85).aspx"/> File Mapping Security and Access Rights.</param>
        /// <param name="inheritHandle">If this parameter is TRUE, a process created by the CreateProcess function can inherit the handle; 
        /// otherwise, the handle cannot be inherited.</param>
        /// <param name="name">The name of the file mapping object to be opened. 
        /// If there is an open handle to a file mapping object by this name and the security descriptor on the mapping object does not conflict with the dwDesiredAccess parameter, 
        /// the open operation succeeds.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified file mapping object.</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int desiredAccess, bool inheritHandle, String name);

        /// <summary>
        /// Maps a view of a file mapping into the address space of a calling Windows Store app.
        /// </summary>
        /// <param name="fileMappingObject">A handle to a file mapping object.</param>
        /// <param name="desiredAccess">The type of access to a file mapping object, which determines the protection of the pages.</param>
        /// <param name="fileOffsetHigh">A high-order DWORD of the file offset where the view begins.</param>
        /// <param name="fileOffsetLow">A low-order DWORD of the file offset where the view is to begin. 
        /// The combination of the high and low offsets must specify an offset within the file mapping. 
        /// They must also match the memory allocation granularity of the system. 
        /// That is, the offset must be a multiple of the allocation granularity. 
        /// To obtain the memory allocation granularity of the system, use the GetSystemInfo function, 
        /// which fills in the members of a SYSTEM_INFO structure.</param>
        /// <param name="numBytesToMap">The number of bytes of a file mapping to map to the view. 
        /// All bytes must be within the maximum size specified by CreateFileMapping.</param>
        /// <returns>If the function succeeds, the return value is the starting address of the mapped view.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr fileMappingObject, int desiredAccess, int fileOffsetHigh, int fileOffsetLow, IntPtr numBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        private static byte[] _depthBytes = new byte[DepthFileSize]; // Unique allocation to avoid constant (and costly) garbage collections.
        private static byte[] _colorBytes = new byte[ColorFileSize];

        /// <summary>
        /// Reads kinect's video stream data from the file mapping created by the kinect data transmitter.
        /// </summary>
        public static byte[] GetVideoStreamData()
        {
            return ReadFromMappedFile(ColorFileName, _colorBytes);
        }
        /// <summary>
        /// Reads kinect's depth stream data from the file mapping created by the kinect data transmitter.
        /// </summary>
        public static byte[] GetDepthStreamData()
        {
            return ReadFromMappedFile(DepthFileName, _depthBytes);
        }

        private static byte[] ReadFromMappedFile(string filename, byte[] bytesBuffer)
        {
            IntPtr handle = IntPtr.Zero;
            IntPtr pointer = IntPtr.Zero;

            try
            {
                handle = OpenFileMapping(ReadAccess, false, filename);
                pointer = MapViewOfFile(handle, ReadAccess, 0, 0, new IntPtr(bytesBuffer.Length));
                Marshal.Copy(pointer, bytesBuffer, 0, bytesBuffer.Length);
            }
            catch (Exception e)
            {
                bytesBuffer = null;
            }
            finally
            {
                try
                {
                    if (handle != IntPtr.Zero)
                    {
                        UnmapViewOfFile(pointer);
                        CloseHandle(handle);
                    }
                }
                catch (Exception)
                {
                }
            }

            return bytesBuffer;
        }

        /// <summary>
        /// Encodes face tracking data for transmission through the data stream.
        /// </summary>
        /// <param name="au0">Animation unit 0.</param>
        /// <param name="au1">Animation unit 1.</param>
        /// <param name="au2">Animation unit 2.</param>
        /// <param name="au3">Animation unit 3.</param>
        /// <param name="au4">Animation unit 4.</param>
        /// <param name="au5">Animation unit 5.</param>
        /// <param name="posX">Head position (x) in meters.</param>
        /// <param name="posY">Head position (y) in meters.</param>
        /// <param name="posZ">Head position (z) in meters.</param>
        /// <param name="rotX">Head rotation (x - euler angle).</param>
        /// <param name="rotY">Head rotation (y - euler angle).</param>
        /// <param name="rotZ">Head rotation (z - euler angle).</param>
        /// <returns>The string that encodes the facetracking information.</returns>
        public static string EncodeFaceTrackingData(float au0, float au1, float au2,
                                                    float au3, float au4, float au5,
                                                    float posX, float posY, float posZ,
                                                    float rotX, float rotY, float rotZ)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}",
                                 FaceTrackingFrameType, au0, au1, au2, au3, au4, au5,
                                 posX, posY, posZ, rotX, rotY, rotZ);
        }

        /// <summary>
        /// Encodes skeleton data to transmission.
        /// </summary>
        public static string EncodeSkeletonData(JointData[] jointsData)
        {
            if (jointsData == null)
            {
                return EncodeError("EncodeSkeletonData: joint data is null.");
            }

            _stringBuilder.Remove(0, _stringBuilder.Length);
            _stringBuilder.Append(SkeletonFrameType + "|");
            foreach (var jointData in jointsData)
            {
                if (jointData.State == TrackingState.NotTracked)
                {
                    continue;
                }

                // state x y z qx qy qz qw 
                _stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6} {7} {8} ",
                                            (int)jointData.JointId, (int)jointData.State, jointData.PositionX, jointData.PositionY, jointData.PositionZ,
                                            jointData.QuaternionX, jointData.QuaternionY, jointData.QuaternionZ, jointData.QuaternionW);
            }
            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Encodes skeleton data to transmission.
        /// </summary>
        public static string EncodeLeftHandData(int handState)
        {
            /*
            if (jointsData == null)
            {
                return EncodeError("EncodeSkeletonData: joint data is null.");
            }
             * */

            _stringBuilder.Remove(0, _stringBuilder.Length);
            _stringBuilder.Append(HandLeftType + "|");
            _stringBuilder.Append(handState);
            
            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Encodes skeleton data to transmission.
        /// </summary>
        public static string EncodeRightHandData(int handState)
        {
            /*
            if (jointsData == null)
            {
                return EncodeError("EncodeSkeletonData: joint data is null.");
            }
             * */

            _stringBuilder.Remove(0, _stringBuilder.Length);
            _stringBuilder.Append(HandRightType + "|");
            _stringBuilder.Append(handState);

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the part of the data that contains content.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetDataContent(string data)
        {
            // We send datablocks of the type: 'FrameType(char)'|Content
            return data.Substring(2);
        }

        /// <summary>
        /// Decodes face tracking data received from the data stream into meaningful values.
        /// </summary>
        /// <param name="data">The data that encodes the facetracking information.</param>
        /// <param name="au0">Animation unit 0.</param>
        /// <param name="au1">Animation unit 1.</param>
        /// <param name="au2">Animation unit 2.</param>
        /// <param name="au3">Animation unit 3.</param>
        /// <param name="au4">Animation unit 4.</param>
        /// <param name="au5">Animation unit 5.</param>
        /// <param name="posX">Head position (x) in meters.</param>
        /// <param name="posY">Head position (y) in meters.</param>
        /// <param name="posZ">Head position (z) in meters.</param>
        /// <param name="rotX">Head rotation (x - euler angle).</param>
        /// <param name="rotY">Head rotation (y - euler angle).</param>
        /// <param name="rotZ">Head rotation (z - euler angle).</param>
        public static void DecodeFaceTrackingData(string data, out float au0, out float au1, out float au2,
                                                  out float au3, out float au4, out float au5,
                                                  out float posX, out float posY, out float posZ,
                                                  out float rotX, out float rotY, out float rotZ)
        {
            string[] tokens = data.Split(' ');
            au0 = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            au1 = float.Parse(tokens[1], CultureInfo.InvariantCulture);
            au2 = float.Parse(tokens[2], CultureInfo.InvariantCulture);
            au3 = float.Parse(tokens[3], CultureInfo.InvariantCulture);
            au4 = float.Parse(tokens[4], CultureInfo.InvariantCulture);
            au5 = float.Parse(tokens[5], CultureInfo.InvariantCulture);
            posX = float.Parse(tokens[6], CultureInfo.InvariantCulture);
            posY = float.Parse(tokens[7], CultureInfo.InvariantCulture);
            posZ = float.Parse(tokens[8], CultureInfo.InvariantCulture);
            rotX = float.Parse(tokens[9], CultureInfo.InvariantCulture);
            rotY = float.Parse(tokens[10], CultureInfo.InvariantCulture);
            rotZ = float.Parse(tokens[11], CultureInfo.InvariantCulture);
        }


        /// <summary>
        /// Decodes the left hand status data received from the data stream
        /// </summary>
        public static void DecodeLeftHandData(string data, int leftHandStatus)
        {

        }

        /// <summary>
        /// Decodes the left hand status data received from the data stream
        /// </summary>
        public static void DecodeRightHandData(string data, int rightHandStatus)
        {

        }

        /// <summary>
        /// Decodes the skeleton data received from the data stream into joint positions.
        /// </summary>
        public static void DecodeSkeletonData(string data, JointData[] jointsData)
        {

            //Console.WriteLine(Converter.EncodeInfo("DecodeSkeletonData in operation..."));

            const int jointsNumber = (int)JointType.NumberOfJoints;
            //Console.WriteLine(Converter.EncodeInfo(jointsNumber + " total joints..."));
            if (jointsData == null || jointsData.Length != jointsNumber)
            {
                throw new Exception("DecodeSkeletonData is expecting a JointData[] buffer big enough to hold the data.");
            }

            for (int i = 0; i < jointsData.Length; i++)
            {
                jointsData[i].State = TrackingState.NotTracked;
            }

            string[] tokens = data.Split(' ');
            const int elementsNumber = 9;
            for (int i = 0; i < tokens.Length / elementsNumber; i++)
            {
                int jointId = int.Parse(tokens[i * elementsNumber], CultureInfo.InvariantCulture);
                jointsData[jointId].State = (TrackingState)int.Parse(tokens[i * elementsNumber + 1], CultureInfo.InvariantCulture);
                jointsData[jointId].PositionX = float.Parse(tokens[i * elementsNumber + 2], CultureInfo.InvariantCulture);
                jointsData[jointId].PositionY = float.Parse(tokens[i * elementsNumber + 3], CultureInfo.InvariantCulture);
                jointsData[jointId].PositionZ = float.Parse(tokens[i * elementsNumber + 4], CultureInfo.InvariantCulture);
                jointsData[jointId].QuaternionX = float.Parse(tokens[i * elementsNumber + 5], CultureInfo.InvariantCulture);
                jointsData[jointId].QuaternionY = float.Parse(tokens[i * elementsNumber + 6], CultureInfo.InvariantCulture);
                jointsData[jointId].QuaternionZ = float.Parse(tokens[i * elementsNumber + 7], CultureInfo.InvariantCulture);
                jointsData[jointId].QuaternionW = float.Parse(tokens[i * elementsNumber + 8], CultureInfo.InvariantCulture);
             }
        }

        /// <summary>
        /// Encodes an error message to be sent through the data stream.
        /// </summary>
        public static string EncodeError(string message)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}", ErrorType, message);
        }

        public static string EncodePingData()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|", PingType);
        }

        /// <summary>
        /// Encodes information messages.
        /// </summary>
        public static string EncodeInfo(string message)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}|{1}", DebugType, message);
        }

        /// <summary>
        /// Checks whether the given data is face tracking data.
        /// </summary>
        public static bool IsFaceTrackingData(string data)
        {
            return data[0] == FaceTrackingFrameType;
        }
        /// <summary>
        /// Checks whether the given data is video frame data.
        /// </summary>
        public static bool IsVideoFrameData(string data)
        {
            return data[0] == VideoFrameType;
        }

        /// <summary>
        /// Checks whether the given data is left hand status.
        /// </summary>
        public static bool IsHandLeftData(string data)
        {
            return data[0] == HandLeftType;
        }

        /// <summary>
        /// Checks whether the given data is left hand status.
        /// </summary>
        public static bool IsHandRightData(string data)
        {
            return data[0] == HandRightType;
        }

        /// <summary>
        /// Checks whether the given data is depth frame data.
        /// </summary>
        public static bool IsDepthFrameData(string data)
        {
            return data[0] == DepthFrameType;
        }
        /// <summary>
        /// Checks whether the given data is an information message.
        /// </summary>
        public static bool IsInformationMessage(string data)
        {
            return data[0] == DebugType;
        }
        /// <summary>
        /// Checks whether the given data is an error message.
        /// </summary>
        public static bool IsError(string data)
        {
            return data[0] == ErrorType;
        }
        /// <summary>
        /// Checks whether the given data is skeleton data.
        /// </summary>
        public static bool IsSkeletonData(string data)
        {
            return data[0] == SkeletonFrameType;
        }
        /// <summary>
        /// Checks whether the given data is a ping data.
        /// </summary>
        public static bool IsPing(string data)
        {
            return data[0] == PingType;
        }
    }


    /// <summary>
    /// The names of the joints.
    /// (This enum is a copy of kinect's JointType, to avoid a necessity of referencing Kinect's dll from within Unity)
    /// </summary>
    public enum JointType
    {
        SpineBase = 0,
        //
        // Summary:
        //     Middle of the spine.
        SpineMid = 1,
        //
        // Summary:
        //     Neck.
        Neck = 2,
        //
        // Summary:
        //     Head.
        Head = 3,
        //
        // Summary:
        //     Left shoulder.
        ShoulderLeft = 4,
        //
        // Summary:
        //     Left elbow.
        ElbowLeft = 5,
        //
        // Summary:
        //     Left wrist.
        WristLeft = 6,
        //
        // Summary:
        //     Left hand.
        HandLeft = 7,
        //
        // Summary:
        //     Right shoulder.
        ShoulderRight = 8,
        //
        // Summary:
        //     Right elbow.
        ElbowRight = 9,
        //
        // Summary:
        //     Right wrist.
        WristRight = 10,
        //
        // Summary:
        //     Right hand.
        HandRight = 11,
        //
        // Summary:
        //     Left hip.
        HipLeft = 12,
        //
        // Summary:
        //     Left knee.
        KneeLeft = 13,
        //
        // Summary:
        //     Left ankle.
        AnkleLeft = 14,
        //
        // Summary:
        //     Left foot.
        FootLeft = 15,
        //
        // Summary:
        //     Right hip.
        HipRight = 16,
        //
        // Summary:
        //     Right knee.
        KneeRight = 17,
        //
        // Summary:
        //     Right ankle.
        AnkleRight = 18,
        //
        // Summary:
        //     Right foot.
        FootRight = 19,
        //
        // Summary:
        //     Between the shoulders on the spine.
        SpineShoulder = 20,
        //
        // Summary:
        //     Tip of the left hand.
        HandTipLeft = 21,
        //
        // Summary:
        //     Left thumb.
        ThumbLeft = 22,
        //
        // Summary:
        //     Tip of the right hand.
        HandTipRight = 23,
        //
        // Summary:
        //     Right thumb.
        ThumbRight = 24,
        //
        // Summary:
        //     The number of JointType values.

        NumberOfJoints = 25
    }
    
    public enum TrackingState
    {
        NotTracked = 0,
        Inferred,
        Tracked,
    }


    public struct JointData
    {
        public JointType JointId;
        public TrackingState State;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float QuaternionX;
        public float QuaternionY;
        public float QuaternionZ;
        public float QuaternionW;
    }
}
