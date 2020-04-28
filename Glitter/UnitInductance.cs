namespace Glitter
{
    public class UnitInductance
    {

        private double lower;
        private double upper;
        private double horizontal;

        public double Upper { get => upper; private set => upper = value; }
        public double Horizontal { get => horizontal; private set => horizontal = value; }
        public double Lower { get => lower; private set => lower = value; }

        public UnitInductance(double upper, double lower, double horizontal)
        {
            Upper = upper;
            Lower = lower;
            Horizontal = horizontal;
        }
    }

}