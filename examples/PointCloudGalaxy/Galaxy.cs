using System;
using System.Collections.Generic;
using System.Text;

namespace PointCloudGalaxy
{
    // based on: https://itinerantgames.tumblr.com/post/78592276402/a-2d-procedural-galaxy-with-c

    class Galaxy
    {
        public int numArms = 5;

        public float armSeparationDistance => 2 * (float)Math.PI / numArms;

        public float armOffsetMax = 0.5f;
        public float rotationFactor = 5;
        public float randomJitter = 0.02f;

        public float scaleOverPlane = 0.15f;

        public IEnumerable<System.Numerics.Vector3> CreateStarts(int count)
        {
            var randomizer = new Random();

            float RndFloat(double min, double max) { return (float)(randomizer.NextDouble() * (max-min) + min); }

            for (int i = 0; i < count; i++)
            {
                var p = CreateRandomStar(RndFloat);

                yield return p;
            }
        }

        private System.Numerics.Vector3 CreateRandomStar(Func<double, double, float> rndFloat)
        {
            // Choose a distance from the center of the galaxy.
            float distance = rndFloat(0,1);
            distance = (float)Math.Pow(distance, 2);

            // Choose an angle between 0 and 2 * PI.
            float angle = rndFloat(0, 2 * Math.PI);
            float armOffset = rndFloat(0, armOffsetMax);
            armOffset = armOffset - armOffsetMax / 2;
            armOffset = armOffset * (1 / distance);

            float squaredArmOffset = (float)Math.Pow(armOffset, 2);
            if (armOffset < 0) squaredArmOffset = squaredArmOffset * -1;

            armOffset = squaredArmOffset;

            float rotation = distance * rotationFactor;

            angle = (int)(angle / armSeparationDistance) * armSeparationDistance + armOffset + rotation;

            // Convert polar coordinates to 2D cartesian coordinates.
            var starX = (float)Math.Cos(angle) * distance;
            var starY = (float)Math.Sin(angle) * distance;                        

            // calculate height over galaxy plane
            var starH = rndFloat(-1,1);
            starH *= 1.0f - distance;
            starH *= scaleOverPlane;

            starX += rndFloat(-1, 1) * 0.5f * randomJitter;
            starY += rndFloat(-1, 1) * 0.5f * randomJitter;
            starH += rndFloat(-1, 1) * 0.5f * randomJitter;

            return new System.Numerics.Vector3(starX, starH, starY);
        }
    }
}
