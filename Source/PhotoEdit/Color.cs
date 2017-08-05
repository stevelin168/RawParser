﻿using System;
using System.Runtime.CompilerServices;

namespace PhotoNet
{
    static public class Color
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RgbToHsv(double red, double green, double blue, uint maxValue, out double h, out double s, out double v)
        {
            red /= maxValue;
            green /= maxValue;
            blue /= maxValue;
            double max = Math.Max(red, green);
            max = Math.Max(max, blue);
            double min = Math.Min(red, green);
            min = Math.Min(min, blue);
            v = max;

            if (max == min)
            {
                h = s = 0; // achromatic
            }
            else
            {
                var d = max - min;
                s = d / max;
                if (red == max)
                {
                    h = (green - blue) / d + (green < blue ? 6 : 0);
                }
                else if (green == max)
                {
                    h = (blue - red) / d + 2.0;
                }
                else if (blue == max)
                {
                    h = (red - green) / d + 4.0;
                }
                else
                {
                    h = 0;
                }
            }
            h /= 6;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HsvToRgb(double h, double s, double v, uint maxValue, ref double red, ref double green, ref double blue)
        {
            if (s == 0)
            {
                red = green = blue = v; // achromatic
            }
            else
            {
                h *= 6;
                if (h >= 6) h = 0;     //H must be < 1
                var var_i = (int)h;             //Or ... var_i = floor( var_h )
                var var_1 = v * (1 - s);
                var var_2 = v * (1 - s * (h - var_i));
                var var_3 = v * (1 - s * (1 - (h - var_i)));

                if (var_i == 0)
                {
                    red = v;
                    green = var_3;
                    blue = var_1;
                }
                else if (var_i == 1)
                {
                    red = var_2;
                    green = v;
                    blue = var_1;
                }
                else if (var_i == 2)
                {
                    red = var_1;
                    green = v;
                    blue = var_3;
                }
                else if (var_i == 3)
                {
                    red = var_1;
                    green = var_2;
                    blue = v;
                }
                else if (var_i == 4)
                {
                    red = var_3;
                    green = var_1;
                    blue = v;
                }
                else
                {
                    red = v;
                    green = var_1;
                    blue = var_2;
                }
            }
            red *= maxValue;
            green *= maxValue;
            blue *= maxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RgbToHsl(double red, double green, double blue, uint maxValue, out double h, out double s, out double l)
        {
            red /= maxValue;
            green /= maxValue;
            blue /= maxValue;
            double max = Math.Max(red, green);
            max = Math.Max(max, blue);
            double min = Math.Min(red, green);
            min = Math.Min(min, blue);
            l = (max + min) / 2.0;

            if (max == min)
            {
                h = s = 0; // achromatic
            }
            else
            {
                var d = max - min;
                s = l >= 0.5 ? d / (2.0 - max - min) : d / (max + min);
                if (red == max)
                {
                    h = (green - blue) / d + (green < blue ? 6 : 0);
                }
                else if (green == max)
                {
                    h = (blue - red) / d + 2.0;
                }
                else if (blue == max)
                {
                    h = (red - green) / d + 4.0;
                }
                else
                {
                    h = 0;
                }
            }
            h /= 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void HslToRgb(double h, double s, double l, uint maxValue, ref double red, ref double green, ref double blue)
        {
            if (s == 0)
            {
                red = green = blue = l; // achromatic
            }
            else
            {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                red = Hue2Rgb(p, q, h + 1 / 3.0);
                green = Hue2Rgb(p, q, h);
                blue = Hue2Rgb(p, q, h - 1 / 3.0);
            }
            red *= maxValue;
            green *= maxValue;
            blue *= maxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Hue2Rgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            else if (t > 1) t -= 1;

            if (t < 1 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 0.5) return q;
            if (t < 2 / 3.0) return p + (q - p) * (2 / 3.0 - t) * 6.0;
            return p;
        }
    }
}
