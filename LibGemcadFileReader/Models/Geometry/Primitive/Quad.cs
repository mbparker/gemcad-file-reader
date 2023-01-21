namespace LibGemcadFileReader.Models.Geometry.Primitive
{
    public class Quad : Polygon
    {
        public Quad()
            : base(4)
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

        public PolygonVertex P4
        {
            get
            {
                return this[3];
            }
            set
            {
                this[3] = value;
            }
        }
    }
}