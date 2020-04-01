using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base
{
    public class CLBSettings
    {
        public CLBSettings(string layerName)
        {
            LayerName = layerName;
        }

        public string LayerName { get; }
    }
}
