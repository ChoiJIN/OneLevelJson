﻿using System.Drawing;

namespace OneLevelJson
{
    static class State
    {
        public static Log log = new Log();
        public static Model.Layer SelectedLayer;
        public static Model.Component SelectedComponent;

        public static Point PointFlip(Point p)
        {
            return Point.Empty;
        }
    }
}
