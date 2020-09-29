using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Driver.PhillipsHue.Models;

namespace Driver.PhillipsHue
{
    public static class LightCommandExtensions
    {

        public static LightCommand SetColor(this LightCommand lightCommand, RGBColor color)
        {
            if (lightCommand == null)
                throw new ArgumentNullException(nameof(lightCommand));

            var hsb = color.GetHSB();
            lightCommand.Brightness = (byte)hsb.Brightness;
            lightCommand.Hue = hsb.Hue;
            lightCommand.Saturation = hsb.Saturation;

            return lightCommand;
        }

    }
}
