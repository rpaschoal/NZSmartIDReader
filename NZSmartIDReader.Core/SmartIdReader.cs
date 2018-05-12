using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZSmartIDReader.Core
{
    public abstract class SmartIdReader
    {
        protected readonly double PredictionThreashold;

        public SmartIdReader(string samplesDirectory, double predictionThreshold)
        {
            if (String.IsNullOrWhiteSpace(AzureCredentials.TrainingKey) || String.IsNullOrWhiteSpace(AzureCredentials.PredictionKey))
            {
                throw new ArgumentNullException($"You need both training and prediction keys to use this library. Set them using the {nameof(AzureCredentials)} static class.");
            }

            PredictionThreashold = predictionThreshold;

            Train(samplesDirectory);
        }

        abstract internal void Train(string samplesDirectory);
    }
}
