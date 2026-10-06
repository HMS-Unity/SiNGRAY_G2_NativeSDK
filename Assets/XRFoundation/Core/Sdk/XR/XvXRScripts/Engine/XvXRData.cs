using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Assets.XvXRScripts.Engine
{

    [StructLayout(LayoutKind.Sequential)]
    public struct XvXROpticalParameter_t
    {
       public float fov_left; //Degrees
       public float fov_right;
       public float fov_top;
       public float fov_bottom;
     
       [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
       public float[] red_coff;     //Distortion coefficients
      
       [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
       public float[] green_coff;
  
       [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
       public float[] blue_coff;
      
       public float red_effective_focal_length;  //Effective focal length
       public float green_effective_focal_length;
       public float blue_effective_focal_length;
       public float screen_width_physics;        //Physical screen width
       public float screen_height_physics;       //Physical screen height
       public float screen_width_pixels;         //In pixels
       public float screen_height_pixels;        //In pixels
        
       public float left_eye_center_x;           //Left-eye center x, relative to the top-left screen corner
       public float left_eye_center_y;           //Left-eye center y, relative to the top-left screen corner
       public float right_eye_center_x;          //Right-eye center x, relative to the top-left screen corner
       public float right_eye_center_y;          //Right-eye center y, relative to the top-left screen corner
       public float separation; // Center to center. 
       public float screenDistance; // Distance from lens center to the phone screen.
       public float bottomOffset; // Offset of lens center from top or bottom
        
       public int renderType; //0,cardboard render
    };

    [StructLayout(LayoutKind.Sequential)]
    struct XvXRHmdState_t
    {
        Int64 timestamp; //ms

        /* State of driver pose, in meters*/
        /* Position of the driver tracking reference in driver world space
        * +[0] (x) is right
        * +[1] (y) is up
        * -[2] (z) is forward
        */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        float[] position; //x,y,z
        /* Orientation of the tracker, represented as a quaternion */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        float[] rotation; //w,x,y,z

        /* Acceleration meter/second^2*/
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        float[] accelerometer; //ax,ay,az

        /* Angular velocity of the pose in axis-angle
        * representation. The direction is the angle of
        * rotation and the magnitude is the angle around
        * that axis in radians/second. */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        float []gyroscope; //gx,gy,gz
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        float []geomagnetism; //uT

        int tracking_result;
    };
}
