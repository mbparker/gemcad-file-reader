namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class Triangle : Polygon
    {
        public Triangle()
            : base(3)
        {
        }

        public PolygonVertex P1
        {
            get
            {
                return this[0];
            }
            set
            {
                this[0] = value;
            }
        }

        public PolygonVertex P2
        {
            get
            {
                return this[1];
            }
            set
            {
                this[1] = value;
            }
        }

        public PolygonVertex P3
        {
            get
            {
                return this[2];
            }
            set
            {
                this[2] = value;
            }
        }
    }
}