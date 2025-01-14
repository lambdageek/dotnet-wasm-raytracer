﻿using System;
using System.Runtime.Intrinsics;

namespace RayTracer
{
    /// <summary>
    /// Contains various mathematic helper methods for scalars and vectors
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Clamps the given value between min and max
        /// </summary>
        public static float Clamp(float value, float min, float max)
        {
            return value > max ? max : value < min ? min : value;
        }

        /// <summary>
        /// Linearly interpolates between two values, based on t
        /// </summary>
        public static float Lerp(float from, float to, float t)
        {
            return (from * (1 - t)) + (to * t);
        }

        /// <summary>
        /// Returns the maximum of the given set of values
        /// </summary>
        public static float Max(params float[] values)
        {
            float max = values[0];
            for (int g = 1; g < values.Length; g++)
            {
                if (values[g] > max)
                {
                    max = values[g];
                }
            }
            return max;
        }

        /// <summary>
        /// Converts an angle from degrees to radians.
        /// </summary>
        internal static float DegreesToRadians(float angleInDegrees)
        {
            var radians = (float)((angleInDegrees / 360f) * 2 * Math.PI);
            return radians;
        }

        public static readonly Vector128<float> RightVector = Vector128.Create(1f, 0f, 0f, 0f);
        public static readonly Vector128<float> UpVector = Vector128.Create(0f, 1f, 0f, 0f);
        public static readonly Vector128<float> ForwardVector = Vector128.Create(0f, 0f, 1f, 0f);

        public static Vector128<float> CrossProduct(Vector128<float> left, Vector128<float> right)
        {
#if  NET8_0_OR_GREATER
            // https://geometrian.com/programming/tutorials/cross-product/index.php
            // "Method 5"
            Vector128<int> shuffleMask0 = Vector128.Create(1, 2, 0, 3); //Vector128.Create(3, 0, 2, 1);
            Vector128<float> tmp0 = Vector128.Shuffle(left, shuffleMask0);
            Vector128<int> shuffleMask1 = Vector128.Create(2, 0, 1, 3); //Vector128.Create(3, 1, 0, 2);
            Vector128<float> tmp1 = Vector128.Shuffle(right, shuffleMask1);
            Vector128<float> tmp2 = tmp0 * right;
            Vector128<float> tmp3 = tmp0 * tmp1;
            Vector128<float> tmp4 = Vector128.Shuffle(tmp2, shuffleMask0);
            return tmp3 - tmp4;
#else
            return Vector128.Create(
                left.Y() * right.Z() - left.Z() * right.Y(),
                left.Z() * right.X() - left.X() * right.Z(),
                left.X() * right.Y() - left.Y() * right.X(),
                0);
#endif  
        }

        //public static float Magnitude(this Vector128<float> v)
        //{
        //    return (float)Math.Abs(Math.Sqrt(Vector128.Dot(v,v)));
        //}

        //public static Vector128<float> Normalized(this Vector128<float> v)
        //{
        //    var mag = v.Magnitude();
        //    if (mag != 1)
        //    {
        //        return v / new Vector128<float>(mag);
        //    }
        //    else
        //    {
        //        return v;
        //    }
        //}

        public static float Distance(Vector128<float> first, Vector128<float> second)
        {
            return (first - second).Magnitude();
        }

        public static Vector128<float> Projection(Vector128<float> projectedVector, Vector128<float> directionVector)
        {
            //var mag = Vector128.Dot(projectedVector, directionVector.Normalize());
            var mag = projectedVector.DotR(directionVector.Normalize());
            return directionVector * mag;
        }
    }
}
