using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.WFC
{
    public class ModelState
    {
        public Dictionary<int, bool>[] wave;
        public int[][][] compatible;
        public int[] observed;
        public Dictionary<string, double> sumOfWeights;
        public Dictionary<string, double> sumOfWeightLogWeights;
        public double[] sumsOfWeights;
        public double[] sumsOfWeightLogWeights;
        public double[] entropies;
    }
}
