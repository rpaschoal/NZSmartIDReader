using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NZSmartIDReader.Core
{
    public abstract class SmartIdReader
    {
        protected readonly double PredictionThreashold;
        private readonly string metadataFileName = "TrainingMetaData.ai";

        public SmartIdReader(string samplesDirectory, double predictionThreshold)
        {
            if (String.IsNullOrWhiteSpace(AzureCredentials.TrainingKey) || String.IsNullOrWhiteSpace(AzureCredentials.PredictionKey))
            {
                throw new ArgumentNullException($"You need both training and prediction keys to use this library. Set them using the {nameof(AzureCredentials)} static class.");
            }

            PredictionThreashold = predictionThreshold;

            if (NeedsTraining(samplesDirectory))
            {
                Train(samplesDirectory);
                File.WriteAllText(MetadataFullPath(samplesDirectory), GetMostRecentTrainingFileDate(samplesDirectory).ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            }

            IsInitialized = true;
        }

        abstract internal void Train(string samplesDirectory);

        public bool IsInitialized
        {
            get; private set;
        }

        private string MetadataFullPath(string samplesDirectory)
        {
            return Path.Combine(samplesDirectory, metadataFileName);
        }

        private DateTime GetMostRecentTrainingFileDate(string samplesDirectory)
        {
            var trainingFilesInfo = new DirectoryInfo(samplesDirectory).GetFiles("*", SearchOption.AllDirectories)
                .Where(f => !f.Name.EndsWith(".ai"))
                .OrderByDescending(f => f.CreationTimeUtc > f.LastWriteTimeUtc ? f.CreationTimeUtc : f.LastWriteTimeUtc);

            if (trainingFilesInfo.Any())
            {
                var mostUpdatedFile = trainingFilesInfo.FirstOrDefault();

                return mostUpdatedFile.CreationTimeUtc > mostUpdatedFile.LastWriteTimeUtc ? mostUpdatedFile.CreationTimeUtc : mostUpdatedFile.LastWriteTimeUtc;
            }

            return DateTime.MinValue;
        }

        private bool NeedsTraining(string samplesDirectory)
        {
            if (!File.Exists(MetadataFullPath(samplesDirectory)))
            {
                return true;
            }
            else
            {
                var metadata = File.ReadAllText(MetadataFullPath(samplesDirectory));

                var lastSync = Convert.ToDateTime(metadata);

                return Math.Abs((GetMostRecentTrainingFileDate(samplesDirectory) - lastSync).TotalSeconds) >= 1;
            }
        }
    }
}
