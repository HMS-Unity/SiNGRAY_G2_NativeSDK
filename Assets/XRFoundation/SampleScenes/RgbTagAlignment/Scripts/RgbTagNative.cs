using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Singray.Foundation.SampleScenes
{
    internal static class RgbTagNative
    {
        [DllImport("singray_rgb_tag", CallingConvention = CallingConvention.Cdecl)]
        private static extern int rta_abi_version();
        [DllImport("singray_rgb_tag", CallingConvention = CallingConvention.Cdecl)]
        private static extern int rta_detect(byte[] rgba, int bytes, int width, int height, double[] calibration,
            double edge, [Out] double[] output, int capacity);
        [DllImport("singray_rgb_tag", CallingConvention = CallingConvention.Cdecl)]
        private static extern int rta_project(double[] calibration, double[] rotation, double[] translation,
            double edge, [Out] double[] output);
        internal static void CheckAbi()
        {
            if (rta_abi_version() != 1) throw new InvalidOperationException("RGB tag native ABI mismatch.");
        }
        internal static int Detect(byte[] rgba, int width, int height, double[] k, double[] result) =>
            rta_detect(rgba, rgba.Length, width, height, k, RgbTagAlignmentDemo.EdgeMetres, result, result.Length);
        internal static int Project(double[] k, Matrix4x4 unityCameraFromTag, double[] pixels)
        {
            // R_cv = diag(1,-1,1) * R_unity * diag(1,1,-1).
            var r = new double[9];
            for (int row = 0; row < 3; row++) for (int col = 0; col < 3; col++)
                r[row * 3 + col] = unityCameraFromTag[row, col] * (row == 1 ? -1 : 1) * (col == 2 ? -1 : 1);
            var t = new[] { (double)unityCameraFromTag.m03, -unityCameraFromTag.m13, (double)unityCameraFromTag.m23 };
            return rta_project(k, r, t, RgbTagAlignmentDemo.EdgeMetres, pixels);
        }
    }
}
