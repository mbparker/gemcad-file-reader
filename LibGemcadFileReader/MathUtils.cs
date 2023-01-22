using System;

namespace LibGemcadFileReader
{
    public static class MathUtils
    {
        public static double FilterAngle(double angle)
        {
            double result = angle;
            while (result > 360)
            {
                result -= 360;
            }
            while (result < -360)
            {
                result += 360;
            }
            if (Math.Abs(result) < 0.00000001)
            {
                result = 0;
            }
            return result;
        }
    }
}