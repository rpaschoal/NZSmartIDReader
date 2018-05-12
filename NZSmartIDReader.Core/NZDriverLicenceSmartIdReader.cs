using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;

namespace NZSmartIDReader.Core
{
    public sealed class NZDriverLicenceSmartIdReader : SmartIdReader, ISmartIdReader
    {
        private Project CustomVisionProject { get; set; }

        public NZDriverLicenceSmartIdReader(string samplesDirectory, double predictionThreshold) : base(samplesDirectory, predictionThreshold) {}

        internal override void Train(string samplesDirectory)
        {
            TrainingApi trainingApi = new TrainingApi() { ApiKey = AzureCredentials.TrainingKey };

            Tag tag;
            Tag tag2;

            var existingProjects = trainingApi.GetProjects();

            CustomVisionProject = existingProjects.FirstOrDefault(p => p.Name.Equals(nameof(NZDriverLicenceSmartIdReader)));

            if (CustomVisionProject == null)
            {
                CustomVisionProject = trainingApi.CreateProject(nameof(NZDriverLicenceSmartIdReader));

                // Needs 2 tags otherwise the custom vision API will return BadRequest
                tag = trainingApi.CreateTag(CustomVisionProject.Id, "DriverLicence");
                tag2 = trainingApi.CreateTag(CustomVisionProject.Id, "NZDriverLicence");
            }
            else
            {
                var projectTags = trainingApi.GetTags(CustomVisionProject.Id);

                tag = projectTags.Tags.FirstOrDefault(t => t.Name.Equals("DriverLicence"));
                tag2 = projectTags.Tags.FirstOrDefault(t => t.Name.Equals("NZDriverLicence"));
            }

            // Batch upload files
            var sampleImagesNZ = Directory.GetFiles(Path.Combine(samplesDirectory, "NewZealand")).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img)));
            var sampleImagesGlobal = Directory.GetFiles(Path.Combine(samplesDirectory, "Global")).Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img)));

            trainingApi.CreateImagesFromFiles(CustomVisionProject.Id, new ImageFileCreateBatch(sampleImagesNZ.ToList(), new List<Guid>() { tag.Id, tag2.Id }));
            trainingApi.CreateImagesFromFiles(CustomVisionProject.Id, new ImageFileCreateBatch(sampleImagesGlobal.ToList(), new List<Guid>() { tag.Id }));

            var trainingIteration = trainingApi.TrainProject(CustomVisionProject.Id);

            // TODO: Improve this: make the initialization async and add a timeout
            while (trainingIteration.Status == "Training")
            {
                Thread.Sleep(1000);

                // Re-query the iteration to get it's updated status
                trainingIteration = trainingApi.GetIteration(CustomVisionProject.Id, trainingIteration.Id);
            }

            trainingIteration.IsDefault = true;
            trainingApi.UpdateIteration(CustomVisionProject.Id, trainingIteration.Id, trainingIteration);

            IsInitialized = true;
        }

        public bool IsInitialized
        {
            get; private set;
        }

        public bool IsValid(Stream imageData)
        {
            if (IsInitialized)
            {
                var predictionEndpoint = new PredictionEndpoint() { ApiKey = AzureCredentials.PredictionKey };

                var result = predictionEndpoint.PredictImage(CustomVisionProject.Id, imageData);

                var overallProbability = result.Predictions.Average(x => x.Probability);

                return overallProbability >= PredictionThreashold;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(NZDriverLicenceSmartIdReader)} is not yet initialized. You can monitor the status of the smart reader via the ${nameof(IsInitialized)} property.");
            }
        }

        public string Read()
        {
            throw new NotImplementedException();
        }
    }
}
