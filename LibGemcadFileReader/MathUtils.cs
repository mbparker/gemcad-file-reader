using System;

namespace LibGemcadFileReader
{
    public static class MathUtils
    {
        public static double FilterAngle(double angle)
        {
            return ClockN(angle, 360.0);
        }
        
        public static double ClockN(double value, double basis)
        {
            basis = Math.Abs(basis);
            double result = value;
            while (result > basis)
            {
                result -= basis;
            }
            while (result < -basis)
            {
                result += basis;
            }
            if (Math.Abs(result) < 0.00000001)
            {
                result = 0;
            }
            return result;
        }
    }
}