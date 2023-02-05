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
                return Vertices[0];
            }
            set
            {
                Vertices[0].Assign(value);
            }
        }

        public PolygonVertex P2
        {
            get
            {
                return Vertices[1];
            }
            set
            {
                Vertices[1].Assign(value);
            }
        }

        public PolygonVertex P3
        {
            get
            {
                return Vertices[2];
            }
            set
            {
                Vertices[2].Assign(value);
            }
        }

        public PolygonVertex P4
        {
            get
            {
                return Vertices[3];
            }
            set
            {
                Vertices[3].Assign(value);
            }
        }
        
        protected override bool PointCountImmutable => true;
    }
}